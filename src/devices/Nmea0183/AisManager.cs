// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Common;
using Iot.Device.Nmea0183.Ais;
using Iot.Device.Nmea0183.AisSentences;
using Iot.Device.Nmea0183.Sentences;
using UnitsNet;

namespace Iot.Device.Nmea0183
{
    public class AisManager : NmeaSinkAndSource
    {
        private readonly bool _throwOnUnknownMessage;
        private AisParser _aisParser;

        /// <summary>
        /// We keep our own position cache, as we need to calculate CPA and TCPA values.
        /// </summary>
        private SentenceCache _cache;

        private ConcurrentDictionary<uint, AisTarget> _targets;

        private object _lock;

        public AisManager(string interfaceName)
        : this(interfaceName, false)
        {
        }

        public AisManager(string interfaceName, bool throwOnUnknownMessage)
            : base(interfaceName)
        {
            _throwOnUnknownMessage = throwOnUnknownMessage;
            _aisParser = new AisParser(throwOnUnknownMessage);
            _cache = new SentenceCache(this);
            _targets = new ConcurrentDictionary<uint, AisTarget>();
            _lock = new object();
        }

        public override void StartDecode()
        {
        }

        public bool TryGetTarget(uint mmsi,
#if NET5_0_OR_GREATER
            [NotNullWhen(true)]
#endif
            out AisTarget target)
        {
            lock (_lock)
            {
                AisTarget? tgt;
                bool ret = _targets.TryGetValue(mmsi, out tgt);
                target = tgt!;
                return ret;
            }
        }

        private Ship GetOrCreateShip(uint mmsi, AisTransceiverClass transceiverClass, bool updateLastSeen = true)
        {
            lock (_lock)
            {
                var ship = GetOrCreateTarget<Ship>(mmsi, x => new Ship(x), updateLastSeen);

                // The transceiver type is derived from the message type (a PositionReportClassA message is obviously only sent by class A equipment)
                if (transceiverClass != AisTransceiverClass.Unknown)
                {
                    ship.TransceiverClass = transceiverClass;
                }

                return ship!;
            }
        }

        private T GetOrCreateTarget<T>(uint mmsi, Func<uint, T> constructor, bool updateLastSeen = true)
        where T : AisTarget
        {
            lock (_lock)
            {
                AisTarget? target;
                T? ship;
                if (TryGetTarget(mmsi, out target) && target is Ship)
                {
                    ship = target as T;
                }
                else
                {
                    // Remove the existing key (this is for the rare case where the same MMSI suddenly changes type from ship to base station or similar.
                    // That should not normally happen, but we need to be robust about it.
                    _targets.TryRemove(mmsi, out _);
                    ship = constructor(mmsi);
                    _targets.TryAdd(mmsi, ship);
                }

                if (updateLastSeen && ship != null)
                {
                    ship.LastSeen = DateTimeOffset.UtcNow;
                }

                return ship!;
            }
        }

        private BaseStation GetOrCreateBaseStation(uint mmsi, AisTransceiverClass transceiverClass, bool updateLastSeen = true)
        {
            return GetOrCreateTarget<BaseStation>(mmsi, x => new BaseStation(mmsi), updateLastSeen);
        }

        private SarAircraft GetOrCreateSarAircraft(uint mmsi, bool updateLastSeen = true)
        {
            return GetOrCreateTarget<SarAircraft>(mmsi, x => new SarAircraft(mmsi), updateLastSeen);
        }

        public IEnumerable<AisTarget> GetTargets()
        {
            lock (_lock)
            {
                return _targets.Values;
            }
        }

        public IEnumerable<T> GetSpecificTargets<T>()
        where T : AisTarget
        {
            lock (_lock)
            {
                return _targets.Values.Where(x => x is T).Cast<T>();
            }
        }

        public override void SendSentence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            _cache.Add(sentence);
            AisMessage? msg = _aisParser.Parse(sentence);
            if (msg == null)
            {
                return;
            }

            Ship? ship;
            lock (_lock)
            {
                switch (msg.MessageType)
                {
                    // These contain the same data
                    case AisMessageType.PositionReportClassA:
                    case AisMessageType.PositionReportClassAAssignedSchedule:
                    case AisMessageType.PositionReportClassAResponseToInterrogation:
                    {
                        PositionReportClassAMessageBase msgPos = (PositionReportClassAMessageBase)msg;
                        ship = GetOrCreateShip(msgPos.Mmsi, msg.TransceiverType);
                        ship.Position = new GeographicPosition(msgPos.Latitude, msgPos.Longitude, 0);
                        if (msgPos.RateOfTurn.HasValue)
                        {
                            // See the cheat sheet at https://gpsd.gitlab.io/gpsd/AIVDM.html
                            double v = msgPos.RateOfTurn.Value / 4.733;
                            ship.RateOfTurn = RotationalSpeed.FromDegreesPerMinute(Math.Sign(v) * v * v); // Square value, keep sign
                        }
                        else
                        {
                            ship.RateOfTurn = null;
                        }

                        if (msgPos.TrueHeading.HasValue)
                        {
                            ship.TrueHeading = Angle.FromDegrees(msgPos.TrueHeading.Value);
                        }
                        else
                        {
                            ship.TrueHeading = null;
                        }

                        ship.CourseOverGround = Angle.FromDegrees(msgPos.CourseOverGround);
                        ship.SpeedOverGround = Speed.FromKnots(msgPos.SpeedOverGround);
                        break;
                    }

                    case AisMessageType.StaticDataReport:
                    {
                        ship = GetOrCreateShip(msg.Mmsi, msg.TransceiverType);
                        if (msg is StaticDataReportPartAMessage msgPartA)
                        {
                            ship.Name = msgPartA.ShipName;
                        }
                        else if (msg is StaticDataReportPartBMessage msgPartB)
                        {
                            ship.CallSign = msgPartB.CallSign;
                            ship.ShipType = msgPartB.ShipType;
                            ship.DimensionToBow = Length.FromMeters(msgPartB.DimensionToBow);
                            ship.DimensionToStern = Length.FromMeters(msgPartB.DimensionToStern);
                            ship.DimensionToPort = Length.FromMeters(msgPartB.DimensionToPort);
                            ship.DimensionToStarboard = Length.FromMeters(msgPartB.DimensionToStarboard);
                        }

                        break;
                    }

                    case AisMessageType.StaticAndVoyageRelatedData:
                    {
                        ship = GetOrCreateShip(msg.Mmsi, msg.TransceiverType);
                        StaticAndVoyageRelatedDataMessage voyage = (StaticAndVoyageRelatedDataMessage)msg;
                        ship.Name = voyage.ShipName;
                        ship.CallSign = voyage.CallSign;
                        ship.Destination = voyage.Destination;
                        ship.Draught = Length.FromMeters(voyage.Draught);
                        var now = DateTimeOffset.UtcNow;
                        if (voyage.IsEtaValid())
                        {
                            int year = now.Year;
                            // If we are supposed to arrive on a month less than the current, this probably means "next year".
                            if (voyage.EtaMonth < now.Month ||
                                (voyage.EtaMonth == now.Month && voyage.EtaDay < now.Day))
                            {
                                year += 1;
                            }

                            try
                            {
                                ship.EstimatedTimeOfArrival = new DateTimeOffset(year, (int)voyage.EtaMonth,
                                    (int)voyage.EtaDay,
                                    (int)voyage.EtaHour, (int)voyage.EtaMinute, 0, TimeSpan.Zero);
                            }
                            catch (Exception x) when (x is ArgumentException || x is ArgumentOutOfRangeException)
                            {
                                // Even when the simple validation above succeeds, the date may still be illegal (e.g. 31 February)
                                ship.EstimatedTimeOfArrival = null;
                            }
                        }
                        else
                        {
                            ship.EstimatedTimeOfArrival = null; // may be deleted by the user
                        }

                        break;
                    }

                    case AisMessageType.StandardClassBCsPositionReport:
                    {
                        StandardClassBCsPositionReportMessage msgPos = (StandardClassBCsPositionReportMessage)msg;
                        ship = GetOrCreateShip(msgPos.Mmsi, msg.TransceiverType);
                        ship.Position = new GeographicPosition(msgPos.Latitude, msgPos.Longitude, 0);
                        ship.RateOfTurn = null;
                        if (msgPos.TrueHeading.HasValue)
                        {
                            ship.TrueHeading = Angle.FromDegrees(msgPos.TrueHeading.Value);
                        }
                        else
                        {
                            ship.TrueHeading = null;
                        }

                        ship.CourseOverGround = Angle.FromDegrees(msgPos.CourseOverGround);
                        ship.SpeedOverGround = Speed.FromKnots(msgPos.SpeedOverGround);
                        break;
                    }

                    case AisMessageType.ExtendedClassBCsPositionReport:
                    {
                        ExtendedClassBCsPositionReportMessage msgPos = (ExtendedClassBCsPositionReportMessage)msg;
                        ship = GetOrCreateShip(msgPos.Mmsi, msg.TransceiverType);
                        ship.Position = new GeographicPosition(msgPos.Latitude, msgPos.Longitude, 0);
                        ship.RateOfTurn = null;
                        if (msgPos.TrueHeading.HasValue)
                        {
                            ship.TrueHeading = Angle.FromDegrees(msgPos.TrueHeading.Value);
                        }
                        else
                        {
                            ship.TrueHeading = null;
                        }

                        ship.CourseOverGround = Angle.FromDegrees(msgPos.CourseOverGround);
                        ship.SpeedOverGround = Speed.FromKnots(msgPos.SpeedOverGround);
                        ship.DimensionToBow = Length.FromMeters(msgPos.DimensionToBow);
                        ship.DimensionToStern = Length.FromMeters(msgPos.DimensionToStern);
                        ship.DimensionToPort = Length.FromMeters(msgPos.DimensionToPort);
                        ship.DimensionToStarboard = Length.FromMeters(msgPos.DimensionToStarboard);
                        ship.ShipType = msgPos.ShipType;
                        ship.Name = msgPos.Name;
                        break;
                    }

                    case AisMessageType.BaseStationReport:
                    {
                        BaseStationReportMessage rpt = (BaseStationReportMessage)msg;
                        var station = GetOrCreateBaseStation(rpt.Mmsi, rpt.TransceiverType, true);
                        station.Position = new GeographicPosition(rpt.Latitude, rpt.Longitude, 0);
                        break;
                    }

                    case AisMessageType.StandardSarAircraftPositionReport:
                    {
                        StandardSarAircraftPositionReportMessage sar = (StandardSarAircraftPositionReportMessage)msg;
                        var sarAircraft = GetOrCreateSarAircraft(sar.Mmsi);
                        // Is the altitude here ellipsoid or geoid? Ships are normally at 0m geoid (unless on a lake, but the AIS system doesn't seem to be designed
                        // for that)
                        sarAircraft.Position = new GeographicPosition(sar.Latitude, sar.Longitude, sar.Altitude);
                        sarAircraft.CourseOverGround = Angle.FromDegrees(sar.CourseOverGround);
                        sarAircraft.Speed = sar.SpeedOverGround == 1023 ? null : Speed.FromKnots(sar.SpeedOverGround);
                        break;
                    }

                    case AisMessageType.AidToNavigationReport:
                    {
                        AidToNavigationReportMessage aton = (AidToNavigationReportMessage)msg;
                        var navigationTarget = GetOrCreateTarget(aton.Mmsi, x => new AidToNavigation(x), true);
                        navigationTarget.Position = new GeographicPosition(aton.Latitude, aton.Longitude, 0);
                        navigationTarget.Name = aton.Name + aton.NameExtension;
                        navigationTarget.DimensionToBow = Length.FromMeters(aton.DimensionToBow);
                        navigationTarget.DimensionToStern = Length.FromMeters(aton.DimensionToStern);
                        navigationTarget.DimensionToPort = Length.FromMeters(aton.DimensionToPort);
                        navigationTarget.DimensionToStarboard = Length.FromMeters(aton.DimensionToStarboard);
                        navigationTarget.OffPosition = aton.OffPosition;
                        navigationTarget.Virtual = aton.VirtualAid;
                        navigationTarget.NavigationalAidType = aton.NavigationalAidType;
                        break;
                    }

                    case AisMessageType.Interrogation:
                    {
                        // Currently nothing to do with these
                        InterrogationMessage interrogation = (InterrogationMessage)msg;
                        break;
                    }

                    case AisMessageType.DataLinkManagement:
                        // not interesting.
                        break;

                    default:
                        if (_throwOnUnknownMessage)
                        {
                            throw new AisParserException(
                                $"Received a message of type {msg.MessageType} which was not handled", sentence.ToNmeaMessage());
                        }

                        break;
                }
            }
        }

        public override void StopDecode()
        {
        }
    }
}

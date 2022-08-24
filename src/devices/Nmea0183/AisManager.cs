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

        private ConcurrentDictionary<uint, Ship> _ships;

        private ConcurrentDictionary<uint, BaseStation> _baseStations;

        private ConcurrentDictionary<uint, AidToNavigation> _aidToNavigationTargets;

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
            _ships = new ConcurrentDictionary<uint, Ship>();
            _baseStations = new ConcurrentDictionary<uint, BaseStation>();
            _aidToNavigationTargets = new ConcurrentDictionary<uint, AidToNavigation>();
            _lock = new object();
        }

        public override void StartDecode()
        {
        }

        public bool TryGetShip(uint mmsi,
#if NET5_0_OR_GREATER
            [NotNullWhen(true)]
#endif
            out Ship? ship)
        {
            lock (_lock)
            {
                return _ships.TryGetValue(mmsi, out ship);
            }
        }

        private Ship GetOrCreateShip(uint mmsi, AisTransceiverClass transceiverClass, bool updateLastSeen = true)
        {
            lock (_lock)
            {
                Ship? ship;
                if (!TryGetShip(mmsi, out ship))
                {
                    ship = new Ship(mmsi);
                    _ships.TryAdd(mmsi, ship);
                }

                if (updateLastSeen && ship != null)
                {
                    ship.LastSeen = DateTimeOffset.UtcNow;

                    // The transceiver type is derived from the message type (a PositionReportClassA message is obviously only sent by class A equipment)
                    if (transceiverClass != AisTransceiverClass.Unknown)
                    {
                        ship.TransceiverClass = transceiverClass;
                    }
                }

                return ship!;
            }
        }

        private BaseStation GetOrCreateBaseStation(uint mmsi, AisTransceiverClass transceiverClass, bool updateLastSeen = true)
        {
            lock (_lock)
            {
                BaseStation? station;
                if (!_baseStations.TryGetValue(mmsi, out station))
                {
                    station = new BaseStation(mmsi);
                    _baseStations.TryAdd(mmsi, station);
                }

                if (updateLastSeen && station != null)
                {
                    station.LastSeen = DateTimeOffset.UtcNow;
                    station.TransceiverClass = transceiverClass;
                }

                return station!;
            }
        }

        public IEnumerable<Ship> GetShips()
        {
            lock (_lock)
            {
                return _ships.Values;
            }
        }

        public IEnumerable<BaseStation> GetBaseStations()
        {
            lock (_lock)
            {
                return _baseStations.Values;
            }
        }

        public List<AidToNavigation> GetAtoNTargets()
        {
            lock (_lock)
            {
                return _aidToNavigationTargets.Values.ToList();
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
                        // Todo
                        break;
                    }

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

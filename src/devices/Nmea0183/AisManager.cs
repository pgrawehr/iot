// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
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
        private static readonly TimeSpan WarningRepeatTimeout = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan CleanupLatency = TimeSpan.FromSeconds(30);

        private readonly bool _throwOnUnknownMessage;

        private AisParser _aisParser;

        /// <summary>
        /// We keep our own position cache, as we need to calculate CPA and TCPA values.
        /// </summary>
        private SentenceCache _cache;

        private ConcurrentDictionary<uint, AisTarget> _targets;

        private ConcurrentDictionary<string, (string Message, DateTimeOffset TimeStamp)> _activeWarnings;

        private object _lock;

        private DateTimeOffset? _lastCleanupCheck;

        /// <summary>
        /// This event fires when a new message (individual or broadcast) is received.
        /// Parameters are: Source MMSI, Destination MMSI (may be 0) and text.
        /// </summary>
        public Action<uint, uint, string>? OnMessage;

        public AisManager(string interfaceName, uint ownMmsi, string ownShipName)
        : this(interfaceName, false, ownMmsi, ownShipName)
        {
        }

        public AisManager(string interfaceName, bool throwOnUnknownMessage, uint ownMmsi, string ownShipName)
            : base(interfaceName)
        {
            OwnMmsi = ownMmsi;
            OwnShipName = ownShipName;
            _throwOnUnknownMessage = throwOnUnknownMessage;
            _aisParser = new AisParser(throwOnUnknownMessage);
            _cache = new SentenceCache(this);
            _targets = new ConcurrentDictionary<uint, AisTarget>();
            _lock = new object();
            _activeWarnings = new ConcurrentDictionary<string, (string Message, DateTimeOffset TimeStamp)>();
            AllowedPositionAge = TimeSpan.FromMinutes(1);
            AutoSendWarnings = true;
            _lastCleanupCheck = null;
            DeleteTargetAfterTimeout = TimeSpan.Zero;
        }

        /// <summary>
        /// The own MMSI
        /// </summary>
        public uint OwnMmsi { get; }

        /// <summary>
        /// The name of the own ship
        /// </summary>
        public string OwnShipName { get; }

        public Length DimensionToBow { get; set; }

        public Length DimensionToStern { get; set; }
        public Length DimensionToPort { get; set; }
        public Length DimensionToStarboard { get; set; }

        /// <summary>
        /// Maximum age of the position record for a given ship to consider it valid.
        /// If this is set to a high value, there's a risk of calculating TCPA/CPA based on outdated data.
        /// </summary>
        public TimeSpan AllowedPositionAge { get; set; }

        /// <summary>
        /// True to have the component automatically generate warning broadcast messages (when in collision range, or when seeing something unexpected,
        /// such as an AIS-Sart target)
        /// </summary>
        public bool AutoSendWarnings { get; set; }

        /// <summary>
        /// If a target has not been updated for this time, it is deleted from the list of targets.
        /// Additionally, client software should consider targets as lost whose <see cref="AisTarget.LastSeen"/> value is older than a minute or so.
        /// A value of 0 or less means infinite.
        /// </summary>
        public TimeSpan DeleteTargetAfterTimeout { get; set; }

        /// <summary>
        /// Which <see cref="SentenceId"/> generated AIS messages should get. Meaningful values are <see cref="AisParser.VdmId"/> or <see cref="AisParser.VdoId"/>.
        /// Default is "VDO"
        /// </summary>
        public SentenceId GeneratedSentencesId
        {
            get
            {
                return _aisParser.GeneratedSentencesId;
            }
            set
            {
                _aisParser.GeneratedSentencesId = value;
            }
        }

        /// <summary>
        /// Gets the data of the own ship (including position and movement vectors) as a ship structure.
        /// </summary>
        /// <param name="ownShip">Receives the data about the own ship</param>
        /// <returns>True in case of success, false if relevant data is outdated or missing. Returns false if the
        /// last received position message is older than <see cref="AllowedPositionAge"/>.</returns>
        public bool GetOwnShipData(out Ship ownShip)
        {
            return GetOwnShipData(out ownShip, DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Gets the data of the own ship (including position and movement vectors) as a ship structure.
        /// </summary>
        /// <param name="ownShip">Receives the data about the own ship</param>
        /// <param name="currentTime">The current time</param>
        /// <returns>True in case of success, false if relevant data is outdated or missing. Returns false if the
        /// last received position message is older than <see cref="AllowedPositionAge"/>.</returns>
        public bool GetOwnShipData(out Ship ownShip, DateTimeOffset currentTime)
        {
            var s = new Ship(OwnMmsi);
            s.Name = OwnShipName;
            s.DimensionToBow = DimensionToBow;
            s.DimensionToStern = DimensionToStern;
            s.DimensionToPort = DimensionToPort;
            s.DimensionToStarboard = DimensionToStarboard;
            if (!_cache.TryGetCurrentPosition(out var position, null, true, out var track, out var sog, out var heading,
                    out var messageTime, currentTime) || (messageTime + AllowedPositionAge) < currentTime)
            {
                s.Position = position ?? new GeographicPosition();
                s.CourseOverGround = track;
                s.SpeedOverGround = sog;
                s.TrueHeading = heading;
                ownShip = s;
                return false;
            }

            s.Position = position!;
            s.CourseOverGround = track;
            s.SpeedOverGround = sog;
            s.TrueHeading = heading;

            ownShip = s;
            return true;
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

            DoCleanup(sentence.DateTime);

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
                        PositionReportClassAToShip(ship, msgPos);

                        CheckIsExceptionalTarget(ship);
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

                        CheckIsExceptionalTarget(ship);
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
                        ship.ImoNumber = voyage.ImoNumber;
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

                        CheckIsExceptionalTarget(ship);
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
                        CheckIsExceptionalTarget(ship);
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
                        CheckIsExceptionalTarget(ship);
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

                    case AisMessageType.AddressedSafetyRelatedMessage:
                    {
                        AddressedSafetyRelatedMessage addressedSafetyRelatedMessage = (AddressedSafetyRelatedMessage)msg;
                        OnMessage?.Invoke(addressedSafetyRelatedMessage.Mmsi, addressedSafetyRelatedMessage.DestinationMmsi, addressedSafetyRelatedMessage.Text);
                        break;
                    }

                    case AisMessageType.SafetyRelatedBroadcastMessage:
                    {
                        SafetyRelatedBroadcastMessage broadcastMessage = (SafetyRelatedBroadcastMessage)msg;
                        OnMessage?.Invoke(broadcastMessage.Mmsi, 0, broadcastMessage.Text);
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

        internal void PositionReportClassAToShip(Ship ship, PositionReportClassAMessageBase positionReport)
        {
            ship.Position = new GeographicPosition(positionReport.Latitude, positionReport.Longitude, 0);
            if (positionReport.RateOfTurn.HasValue)
            {
                // See the cheat sheet at https://gpsd.gitlab.io/gpsd/AIVDM.html
                double v = positionReport.RateOfTurn.Value / 4.733;
                ship.RateOfTurn = RotationalSpeed.FromDegreesPerMinute(Math.Sign(v) * v * v); // Square value, keep sign
            }
            else
            {
                ship.RateOfTurn = null;
            }

            if (positionReport.TrueHeading.HasValue)
            {
                ship.TrueHeading = Angle.FromDegrees(positionReport.TrueHeading.Value);
            }
            else
            {
                ship.TrueHeading = null;
            }

            ship.CourseOverGround = Angle.FromDegrees(positionReport.CourseOverGround);
            ship.SpeedOverGround = Speed.FromKnots(positionReport.SpeedOverGround);
            ship.NavigationStatus = positionReport.NavigationStatus;
        }

        private void CheckIsExceptionalTarget(Ship ship)
        {
            void SendMessage(Ship ship, string type)
            {
                GetOwnShipData(out Ship ownShip); // take in in either case
                Length distance = ownShip.DistanceTo(ship);
                SendWarningMessage(ship.FormatMmsi(), ship.Mmsi,
                    $"{type} Target activated: MMSI {ship.Mmsi} in Position {ship.Position:M1 M1}! Distance {distance}");
            }

            if (AutoSendWarnings == false)
            {
                return;
            }

            MmsiType type = ship.IdentifyMmsiType();
            switch (type)
            {
                case MmsiType.AisSart:
                    SendMessage(ship, "AIS SART");
                    break;
                case MmsiType.Epirb:
                    SendMessage(ship, "EPIRB");
                    break;
                case MmsiType.Mob:
                    SendMessage(ship, "AIS MOB");
                    break;
            }
        }

        /// <summary>
        /// Sends a message with the given <paramref name="messageText"/> as an AIS broadcast message
        /// </summary>
        /// <param name="messageId">Identifies the message. Messages with the same ID are only sent once, until the timeout elapses</param>
        /// <param name="sourceMmsi">Source MMSI, can be 0 if irrelevant/unknown</param>
        /// <param name="messageText">The text of the message. Supports only the AIS 6-bit character set.</param>
        /// <returns>True if the message was sent, false otherwise</returns>
        public bool SendWarningMessage(string messageId, uint sourceMmsi, string messageText)
        {
            if (_activeWarnings.TryGetValue(messageId, out var msg))
            {
                if (msg.TimeStamp + WarningRepeatTimeout > DateTimeOffset.UtcNow)
                {
                    return false;
                }

                _activeWarnings.TryRemove(messageId, out _);
            }

            if (_activeWarnings.TryAdd(messageId, (messageText, DateTimeOffset.UtcNow)))
            {
                SendBroadcastMessage(sourceMmsi, messageText);
                return true;
            }

            return false;
        }

        public void SendBroadcastMessage(uint sourceMmsi, string text)
        {
            SafetyRelatedBroadcastMessage msg = new SafetyRelatedBroadcastMessage();
            msg.Mmsi = sourceMmsi;
            msg.Text = text;
            List<NmeaSentence> sentences = _aisParser.ToSentences(msg);
            foreach (var s in sentences)
            {
                DispatchSentenceEvents(this, s);
            }
        }

        public override void StopDecode()
        {
            _activeWarnings.Clear();
        }

        internal PositionReportClassAMessage ShipToPositionReportClassAMessage(Ship ship)
        {
            PositionReportClassAMessage rpt = new PositionReportClassAMessage();
            rpt.Mmsi = ship.Mmsi;
            rpt.SpeedOverGround = ship.SpeedOverGround.Knots;
            if (ship.RateOfTurn != null)
            {
                // Inverse of the formula above
                double v = ship.RateOfTurn.Value.DegreesPerMinute;
                v = Math.Sign(v) * Math.Sqrt(Math.Abs(v));
                v = v * 4.733;
                rpt.RateOfTurn = (int)Math.Round(v);
            }
            else
            {
                rpt.RateOfTurn = null;
            }

            rpt.CourseOverGround = ship.CourseOverGround.Degrees;
            rpt.Latitude = ship.Position.Latitude;
            rpt.Longitude = ship.Position.Longitude;
            rpt.ManeuverIndicator = ManeuverIndicator.NoSpecialManeuver;
            rpt.NavigationStatus = ship.NavigationStatus;
            if (ship.TrueHeading.HasValue)
            {
                rpt.TrueHeading = (uint)ship.TrueHeading.Value.Degrees;
            }

            return rpt;
        }

        public NmeaSentence SendShipPositionReport(AisTransceiverClass transceiverClass, Ship ship)
        {
            if (transceiverClass == AisTransceiverClass.A)
            {
                PositionReportClassAMessage msg = ShipToPositionReportClassAMessage(ship);
                List<NmeaSentence> sentences = _aisParser.ToSentences(msg);
                if (sentences.Count != 1)
                {
                    throw new InvalidOperationException(
                        $"Encoding the position report for class A returned {sentences.Count} sentences. Exactly 1 expected");
                }

                NmeaSentence single = sentences.Single();

                DispatchSentenceEvents(this, single);
                return single;
            }
            else
            {
                throw new NotSupportedException("Only class A messages can currently be constructed");
            }
        }

        /// <summary>
        /// Regularly scan our database to check for outdated targets. This is done from
        /// the parser thread, so we don't need to create a separate thread just for this.
        /// </summary>
        /// <param name="currentTime">The time of the last packet</param>
        private void DoCleanup(DateTimeOffset currentTime)
        {
            if (DeleteTargetAfterTimeout <= TimeSpan.Zero)
            {
                return;
            }

            // Do if the cleanuplatency has elapsed
            if (_lastCleanupCheck == null || _lastCleanupCheck.Value + CleanupLatency < currentTime)
            {
                lock (_lock)
                {
                    foreach (var t in _targets.Values)
                    {
                        if (t.Age(currentTime) > DeleteTargetAfterTimeout)
                        {
                            _targets.TryRemove(t.Mmsi, out _);
                        }
                    }
                }
            }
        }
    }
}

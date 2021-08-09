using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Iot.Device.Common;
using Iot.Device.Nmea0183;
using Iot.Device.Nmea0183.Sentences;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using UnitsNet;
using UnitsNet.Units;

namespace DisplayControl
{
    public sealed class NmeaSensor : IDisposable
    {
        private readonly MeasurementManager _manager;
        private const string ShipSourceName = "Ship";
        private const string HandheldSourceName = "Handheld";
        private const string OpenCpn = "OpenCpn";
        private const string SignalKOut = "SignalKOut";
        private const string SignalKIn = "SignalK";
        
        /// <summary>
        /// This connects to the ship network (via UART-to-NMEA2000 bridge)
        /// </summary>
        private NmeaParser _parserShipInterface;

        /// <summary>
        /// This connects to the handheld GPS (input) and the Autopilot (output)
        /// </summary>
        private NmeaParser _parserHandheldInterface;

        /// <summary>
        /// OpenCPN is supposed to connect here
        /// </summary>
        private NmeaServer _openCpnServer;

        /// <summary>
        /// Server instance for signal-k
        /// </summary>
        private NmeaServer _signalkServer;

        private SystemClockSynchronizer _clockSynchronizer;
        
        private MessageRouter _router;

        private Stream _streamShip;
        private SerialPort _serialPortShip;
        private Stream _streamHandheld;
        private SerialPort _serialPortHandheld;
        private SentenceCache _cache;

        private Angle? _magneticVariation;
        private GlobalPositioningSystemFixData _lastGgaMessage;
        private RecommendedMinimumNavigationInformation _lastRmcMessage;
        private TrackMadeGood _lastVtgMessage;
        private Temperature? _lastTemperature;
        private RelativeHumidity? _lastHumidity;
        private AutopilotController _autopilot;
        private SensorMeasurement _smoothedTrueWindSpeed;
        private SensorMeasurement _maxWindGusts;
        private CustomData<GeographicPosition> _position;
        private CustomData<int> _numSatellites;
        private CustomData<string> _satStatus;

        private ILogger _logger;

        public NmeaSensor(MeasurementManager manager)
        {
            _manager = manager;
            _magneticVariation = null;
            _smoothedTrueWindSpeed = new SensorMeasurement("Smoothed True Wind Speed", Speed.Zero, SensorSource.WindTrue);
            _maxWindGusts = new SensorMeasurement("Wind Gusts", Speed.Zero, SensorSource.WindTrue);
            _position = new CustomData<GeographicPosition>("Geographic Position", new GeographicPosition(), SensorSource.Position);
            _numSatellites = new CustomData<int>("Number of Sats in view", 0, SensorSource.Position);
            _satStatus = new CustomData<string>("Satellites in View", string.Empty, SensorSource.Position);
            _logger = this.GetCurrentClassLogger();
        }

        public IList<FilterRule> ConstructRules()
        {
            TalkerId yd = new TalkerId('Y', 'D');
            // Note: Order is important. First ones are checked first
            IList<FilterRule> rules = new List<FilterRule>();
            // Drop any incoming sentences from this source, they were processed already (no known use case here)
            // rules.Add(new FilterRule(SignalKIn, TalkerId.Any, SentenceId.Any, new List<string>(), false ));
            // Log just everything, but of course continue processing
            rules.Add(new FilterRule("*", TalkerId.Any, SentenceId.Any, new []{ MessageRouter.LoggingSinkName }, false, true));
            // The time message is required by the time component
            rules.Add(new FilterRule("*", TalkerId.Any, new SentenceId("ZDA"), new []{ _clockSynchronizer.InterfaceName }, false, true));
            // GGA messages from the ship are discarded (the ones from the handheld shall be used instead)
            rules.Add(new FilterRule("*", yd, new SentenceId("GGA"), new List<string>(), false, false));
            // Same applies for this. For some reason, this also gets a different value for the magnetic variation
            rules.Add(new FilterRule("*", yd, new SentenceId("RMC"), new List<string>(), false, false));
            // And these.
            rules.Add(new FilterRule("*", yd, new SentenceId("GLL"), new List<string>(), false, false));
            rules.Add(new FilterRule("*", yd, new SentenceId("VTG"), new List<string>(), false, false));
            // The ship provides these for its GPS signal, but if we forward them i.e to OpenCPN, it gets confused (showing random data 
            // in the satellite plot)
            rules.Add(new FilterRule("*", yd, new SentenceId("GSV"), new List<string>(), false, false));
            rules.Add(new FilterRule("*", yd, new SentenceId("GSA"), new List<string>(), false, false));
            // Anything from the local software (i.e. IMU data, temperature data) is sent to the ship and other nav software
            rules.Add(new FilterRule(MessageRouter.LocalMessageSource, TalkerId.Any, SentenceId.Any, new[] { ShipSourceName, OpenCpn, SignalKOut }, false, true));

            // Anything from SignalK is currently discarded (maybe there are some computed sentences that are useful)
            // Note: This source does not normally generate any data. Input from SignalK is on SignalKIn
            rules.Add(new FilterRule(SignalKOut, TalkerId.Any, SentenceId.Any, new List<string>()));
            // Anything from OpenCpn is distributed everywhere
            rules.Add(new FilterRule(OpenCpn, TalkerId.Any, SentenceId.Any, new [] { SignalKOut, ShipSourceName, HandheldSourceName }));
            // Anything from the ship is sent locally
            rules.Add(new FilterRule(ShipSourceName, TalkerId.Any, SentenceId.Any, new [] { OpenCpn, SignalKOut, MessageRouter.LocalMessageSource }, false, true));
            // Anything from the handheld is sent to our processor
            rules.Add(new FilterRule(HandheldSourceName, TalkerId.Any, SentenceId.Any, new[] { MessageRouter.LocalMessageSource }, false, true));

            // The GPS messages are sent everywhere (as raw)
            string[] gpsSequences = new string[]
            {
                "GGA", "GLL", "RMC", "ZDA", "GSV", "VTG", "GSA"
            };

            foreach (var gpsSequence in gpsSequences)
            {
                rules.Add(new FilterRule(HandheldSourceName, TalkerId.Any, new SentenceId(gpsSequence), new[] { OpenCpn, ShipSourceName, SignalKOut }, true, true));
            }

            string[] navigationSentences = new string[]
            {
                "RMB", "BOD", "XTE", "BWC", "RTE", "APA", "APB", "BWR"
            };

            foreach (var navigationSentence in navigationSentences)
            {
                // Send these from handheld to the nav software, so that this picks up the current destination.
                // signalK is able to do this, OpenCPN is not
                rules.Add(new FilterRule(HandheldSourceName, TalkerId.Any, new SentenceId(navigationSentence), new[] { SignalKOut, OpenCpn }, RemoveNonAsciiFromMessageForSignalk, false, true));
                // And send the result of that nav operation to the ship 
                // TODO: Choose which nav solution to use: Handheld direct, OpenCPN, signalK, depending on who is ready to do so
                // rules.Add(new FilterRule(SignalKIn, new TalkerId('I', 'I'), SentenceId.Any, new []{ ShipSourceName }, true, true));
            }

            // Send the autopilot anything he can use, but only from the ship (we need special filters to send him info from ourselves, if there are any)
            string[] autoPilotSentences = new string[]
            {
                "APB", "APA", "RMB", "XTE", "XTR",
                "BPI", "BWR", "BWC",
                "BER", "BEC", "WDR", "WDC", "BOD", "WCV", "VWR", "VHW"
            };
            foreach (var autopilot in autoPilotSentences)
            {
                // - Maybe we need to be able to switch between using OpenCpn and the Handheld for autopilot / navigation control
                // - For now, we forward anything from our own processor to the real autopilot and the ship (so it gets displayed on the displays)
                rules.Add(new FilterRule(MessageRouter.LocalMessageSource, TalkerId.Any, new SentenceId(autopilot), new []{ HandheldSourceName }, false, true));
            }

            // The messages VWR and VHW (Wind measurement / speed trough water) come from the ship and need to go to the autopilot
            rules.Add(new FilterRule(ShipSourceName, TalkerId.Any, new SentenceId("VWR"), new[] { HandheldSourceName }, true, true));
            rules.Add(new FilterRule(ShipSourceName, TalkerId.Any, new SentenceId("VHW"), new[] { HandheldSourceName }, true, true));

            return rules;
        }

        private NmeaSentence RemoveNonAsciiFromMessageForSignalk(NmeaSinkAndSource source, NmeaSinkAndSource destination, NmeaSentence sentence)
        {
            NmeaSentence correctedMessage = sentence;
            if (sentence is RawSentence)
            {
                // Only the non-raw (recognized) sentences can be manipulated
                return null;
            }

            if (sentence is RecommendedMinimumNavToDestination rmb)
            {
                if (rmb.NextWayPointName.Any(c => c >= 128) || rmb.PreviousWayPointName.Any(c => c >= 128))
                {
                    correctedMessage = new RecommendedMinimumNavToDestination(rmb.DateTime, rmb.CrossTrackError, RemoveNonAscii(rmb.PreviousWayPointName), 
                        RemoveNonAscii(rmb.NextWayPointName), rmb.NextWayPoint, rmb.DistanceToWayPoint.GetValueOrDefault(Length.FromNauticalMiles(99)), 
                        rmb.BearingToWayPoint.GetValueOrDefault(Angle.Zero), rmb.ApproachSpeed.GetValueOrDefault(Speed.Zero), rmb.Arrived);
                }
            }

            if (sentence is BearingOriginToDestination bod)
            {
                if (bod.DestinationName.Any(c => c >= 128) || bod.OriginName.Any(c => c >= 128))
                {
                    correctedMessage = new BearingOriginToDestination(bod.BearingTrue, bod.BearingMagnetic, RemoveNonAscii(bod.OriginName), 
                        RemoveNonAscii(bod.DestinationName));
                }
            }

            if (sentence is BearingAndDistanceToWayPoint bwc)
            {
                if (bwc.NextWayPointName.Any(c => c >= 128))
                {
                    correctedMessage = new BearingAndDistanceToWayPoint(bwc.DateTime, RemoveNonAscii(bwc.NextWayPointName), bwc.NextWayPoint, 
                        bwc.DistanceToWayPoint.GetValueOrDefault(Length.FromNauticalMiles(99)), 
                        bwc.BearingTrueToWayPoint.GetValueOrDefault(Angle.Zero), bwc.BearingMagneticToWayPoint.GetValueOrDefault(Angle.Zero));
                }
            }

            if (sentence is RoutePart rte)
            {
                correctedMessage = new RoutePart(RemoveNonAscii(rte.RouteName), rte.TotalSequences, rte.Sequence, rte.WaypointNames.Select(x => RemoveNonAscii(x)).ToList());
            }

            if (sentence is Waypoint wpl)
            {
                correctedMessage = new Waypoint(wpl.Position, RemoveNonAscii(wpl.Name));
            }

            if (correctedMessage.ToNmeaMessage().Any(x => x >= 128))
            {
                throw new InvalidOperationException();
            }
            return correctedMessage;
        }

        private string RemoveNonAscii(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                switch (c)
                {
                    case 'ä':
                        input = input.Replace("ä", "ae");
                        break;
                    case 'ü':
                        input = input.Replace("ü", "ue");
                        break;
                    case 'ö':
                        input = input.Replace("ö", "oe");
                        break;

                    default:
                        if (c > 127)
                        {
                            input = input.Replace(c, '?');
                        }

                        break;
                }
            }

            return input;
        }

        public void Initialize(SensorFusionEngine fusionEngine)
        {
            _position.UpdateValue(new GeographicPosition());
            _manager.AddRange(new[]
            {
                SensorMeasurement.WindSpeedAbsolute, SensorMeasurement.WindSpeedApparent, SensorMeasurement.WindSpeedTrue, 
                SensorMeasurement.WindDirectionAbsolute, SensorMeasurement.WindDirectionApparent, SensorMeasurement.WindDirectionTrue,
                SensorMeasurement.SpeedOverGround, SensorMeasurement.Track, _position,
                SensorMeasurement.Latitude, SensorMeasurement.Longitude, SensorMeasurement.AltitudeEllipsoid, SensorMeasurement.AltitudeGeoid,
                SensorMeasurement.WaterDepth, SensorMeasurement.WaterTemperature, SensorMeasurement.SpeedTroughWater, 
                SensorMeasurement.UtcTime, _smoothedTrueWindSpeed, _maxWindGusts, _numSatellites, _satStatus
            });

            _serialPortShip = new SerialPort("/dev/ttyAMA1", 115200);
            _serialPortShip.Open();
            _streamShip = _serialPortShip.BaseStream;
            _parserShipInterface = new NmeaParser(ShipSourceName, _streamShip, _streamShip);
            _parserShipInterface.OnParserError += OnParserError;
            _parserShipInterface.StartDecode();

            _serialPortHandheld = new SerialPort("/dev/ttyAMA2", 4800);
            _serialPortHandheld.Open();

            _streamHandheld = _serialPortHandheld.BaseStream;

            _parserHandheldInterface = new NmeaParser(HandheldSourceName, _streamHandheld, _streamHandheld);
            _parserHandheldInterface.OnParserError += OnParserError;
            _parserHandheldInterface.StartDecode();

            _openCpnServer = new NmeaServer(OpenCpn, IPAddress.Any, 10100);
            _openCpnServer.OnParserError += OnParserError;
            _openCpnServer.StartDecode();

            _signalkServer = new NmeaServer(SignalKOut, IPAddress.Any, 10101);
            _signalkServer.OnParserError += OnParserError;
            _signalkServer.StartDecode();

            // TODO: This source is probably not required
            //_signalKClient = new TcpClient("127.0.0.1", 10110);
            //_signalKClientParser = new NmeaParser(SignalKIn, _signalKClient.GetStream(), _signalKClient.GetStream());
            //_signalKClientParser.OnParserError += OnParserError;
            // _signalKClientParser.ExclusiveTalkerId = new TalkerId('I', 'I');
            // _signalKClientParser.StartDecode();

            _clockSynchronizer = new SystemClockSynchronizer();
            _clockSynchronizer.StartDecode();

            _router = new MessageRouter(new LoggingConfiguration() { Path = "/home/pi/projects/ShipLogs", MaxFileSize = 1024 * 1024 * 5 , SortByDate = true });

            _cache = new SentenceCache(_router);
            _autopilot = new AutopilotController(_router, _router, _cache);

            _router.AddEndPoint(_parserShipInterface);
            _router.AddEndPoint(_parserHandheldInterface);
            _router.AddEndPoint(_openCpnServer);
            _router.AddEndPoint(_signalkServer);
            _router.AddEndPoint(_clockSynchronizer);
            // _router.AddEndPoint(_signalKClientParser);

            _router.OnNewSequence += ParserOnNewSequence;
            foreach (var rule in ConstructRules())
            {
                _router.AddFilterRule(rule);
            }

            _manager.ConfigureHistory(SensorMeasurement.WindSpeedTrue, TimeSpan.Zero, TimeSpan.FromMinutes(5));

            fusionEngine.RegisterHistoryOperation(SensorMeasurement.WindSpeedTrue,
                (input, manager) =>
                {
                    var list = manager.ObtainHistory(input, TimeSpan.FromSeconds(4), TimeSpan.Zero);
                    if (list.Count == 0)
                    {
                        return null;
                    }
                    return list.AverageValue();
                }, _smoothedTrueWindSpeed, TimeSpan.FromSeconds(2));

            // This is the same value
            fusionEngine.RegisterFusionOperation(new[] { SensorMeasurement.WindSpeedTrue },
                (args) => (args[0].Value, false), SensorMeasurement.WindSpeedAbsolute, TimeSpan.FromSeconds(3));

            // Calculate true wind direction (in geographic directions) from relative direction.
            fusionEngine.RegisterFusionOperation(new[] { SensorMeasurement.WindDirectionTrue, SensorMeasurement.Heading },
                (args) =>
                {
                    if (args[0].Status.HasFlag(SensorMeasurementStatus.NoData))
                    {
                        return (null, true);
                    }

                    if (args[0].TryGetAs(out Angle dir) && args[1].TryGetAs(out Angle hdg))
                    {
                        Angle result = (dir + hdg).Normalize(true);
                        return (result, false);
                    }

                    return (null, false);
                }, SensorMeasurement.WindDirectionAbsolute, TimeSpan.FromSeconds(3));

            fusionEngine.RegisterHistoryOperation(SensorMeasurement.WindSpeedTrue,
                (input, manager) =>
                {
                    var list = manager.ObtainHistory(input, TimeSpan.FromSeconds(30), TimeSpan.Zero);
                    if (list.Count == 0)
                    {
                        return null;
                    }
                    return list.MaxValue();
                }, _maxWindGusts, TimeSpan.FromSeconds(5));

            _router.StartDecode();
            _autopilot.Start();
        }

        private void ParserOnNewSequence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            Stopwatch sw = Stopwatch.StartNew();
            switch (sentence)
            {
                case GlobalPositioningSystemFixData gga:
                {
                    if (gga.Valid)
                    {
                        if (_lastGgaMessage != null && _lastGgaMessage.Age < TimeSpan.FromSeconds(0.5))
                        {
                            break;
                        }

                        _lastGgaMessage = gga;
                        _position.UpdateValue(gga.Position);
                        _manager.UpdateValues(new[] { SensorMeasurement.Latitude, SensorMeasurement.Longitude, SensorMeasurement.AltitudeEllipsoid, SensorMeasurement.AltitudeGeoid },
                            new IQuantity[] { Angle.FromDegrees(gga.LatitudeDegrees.GetValueOrDefault(0)), Angle.FromDegrees(gga.LongitudeDegrees.GetValueOrDefault(0)),
                                Length.FromMeters(gga.EllipsoidAltitude.GetValueOrDefault(0)), Length.FromMeters(gga.GeoidAltitude.GetValueOrDefault(0)) });
                    }
                    else
                    {
                        _manager.UpdateValues(new[] { SensorMeasurement.Latitude, SensorMeasurement.Longitude, SensorMeasurement.AltitudeEllipsoid, SensorMeasurement.AltitudeGeoid },
                            new IQuantity[] { null, null, null, null });
                    }

                    break;
                }
                case RecommendedMinimumNavigationInformation rmc when _lastRmcMessage != null && _lastRmcMessage.Age < TimeSpan.FromSeconds(0.5):
                    break;
                case RecommendedMinimumNavigationInformation rmc:
                {
                    _lastRmcMessage = rmc;
                    _manager.UpdateValues(new[] { SensorMeasurement.SpeedOverGround, SensorMeasurement.Track, SensorMeasurement.MagneticVariation }, 
                        new IQuantity[] { rmc.SpeedOverGround, rmc.TrackMadeGoodInDegreesTrue, rmc.MagneticVariationInDegrees });

                    break;
                }
                case TrackMadeGood vtg when _lastVtgMessage != null && _lastVtgMessage.Age < TimeSpan.FromSeconds(0.5):
                    break;
                case TrackMadeGood vtg:
                    _lastVtgMessage = vtg;
                    _manager.UpdateValues(new[] { SensorMeasurement.SpeedOverGround, SensorMeasurement.Track },
                        new IQuantity[] { vtg.Speed, vtg.CourseOverGroundTrue });
                    break;
                case WindSpeedAndAngle mwv when mwv.Relative:
                {
                    if (mwv.Valid)
                    {
                        _manager.UpdateValues(
                            new[] {SensorMeasurement.WindSpeedApparent, SensorMeasurement.WindDirectionApparent},
                            new IQuantity[] {mwv.Speed.ToUnit(SpeedUnit.Knot), mwv.Angle});
                    }
                    else
                    {
                        _manager.UpdateValue(SensorMeasurement.WindSpeedApparent, Speed.Zero,
                            SensorMeasurementStatus.SensorError);
                        _manager.UpdateValue(SensorMeasurement.WindDirectionApparent, Angle.Zero,
                            SensorMeasurementStatus.SensorError);
                    }

                    break;
                }
                case WindSpeedAndAngle mwv when !mwv.Relative && mwv.Valid:
                    _manager.UpdateValues(
                            new[] {SensorMeasurement.WindSpeedTrue, SensorMeasurement.WindDirectionTrue},
                            new IQuantity[] {mwv.Speed.ToUnit(SpeedUnit.Knot), mwv.Angle});

                    break;

                case DepthBelowSurface dpt when dpt.Valid:
                    _manager.UpdateValue(SensorMeasurement.WaterDepth, dpt.Depth);
                    break;

                case WaterSpeedAndAngle vhw when vhw.Valid:
                    _manager.UpdateValue(SensorMeasurement.SpeedTroughWater, vhw.Speed);
                    break;

                case TransducerMeasurement xdr when xdr.Valid:
                    foreach(var ds in xdr.DataSets)
                    {
                        if (ds.DataName == "ENV_WATER_T")
                        {
                            Temperature? temp = ds.AsTemperature();
                            if (temp.HasValue)
                            {
                                _manager.UpdateValue(SensorMeasurement.WaterTemperature, temp);
                            }
                        }
                    }
                    break;
                case TimeDate zda when zda.Valid && zda.DateTime.HasValue:
                    _manager.UpdateValue(SensorMeasurement.UtcTime, zda.DateTime.Value.UtcDateTime);
                    break;
                case SatellitesInView gsv when gsv.Valid && gsv.Sequence == gsv.TotalSequences:
                    var sats = _cache.GetSatellitesInView(out int totalSats);
                    _manager.UpdateValue(_numSatellites, totalSats);
                    string data = string.Join(", ", sats.Select(x => x.Id));
                    _manager.UpdateValue(_satStatus, data);
                    break;
            }

            if (sw.ElapsedMilliseconds > 50)
            {
                _logger.LogError($"Processing message {sentence.GetType().Name} took {sw.ElapsedMilliseconds}");
            }
        }

        private void OnParserError(NmeaSinkAndSource source, string error, NmeaError errorCode)
        {
            _logger.LogError($"Nmea error from {source.InterfaceName}: {error}");
        }

        public void SendImuData(Vector3 value)
        {
            // Z is pitch, Y is roll, X is heading
            if (_router == null)
            {
                return;
            }
            HeadingMagnetic mag = new HeadingMagnetic(value.X);
            _router.SendSentence(mag);

            if (_magneticVariation != null)
            {
                HeadingTrue hdt = new HeadingTrue(value.X + _magneticVariation.Value.Degrees);
                _router.SendSentence(hdt);

                HeadingAndDeviation hdg = new HeadingAndDeviation(Angle.FromDegrees(value.X), null, _magneticVariation.Value);
                _router.SendSentence(hdg);
            }

            var attitude = TransducerMeasurement.FromRollAndPitch(Angle.FromDegrees(value.Y),
                Angle.FromDegrees(value.Z), Angle.FromDegrees(value.X));
            _router.SendSentences(attitude);
        }

        //private string ConvertAngle(double angle)
        //{
        //    // Degrees = X * 57.29 *.0001
        //    int val = (int)Math.Round(angle / 57.29 / 0.0001);
        //    return val.ToString("X4", CultureInfo.InvariantCulture);
        //}

        public void Dispose()
        {
            _autopilot?.Stop();
            _router?.Dispose();
            _router = null;
            _cache?.Clear();

            if (_serialPortShip != null)
            {
                _serialPortShip.Close();
                _parserShipInterface.Dispose();
                _serialPortShip.Dispose();
            }

            if (_serialPortHandheld != null)
            {
                _serialPortHandheld.Close();
                _parserHandheldInterface.Dispose();
                _serialPortHandheld.Dispose();
            }

            _openCpnServer?.Dispose();
            _openCpnServer = null;

            _signalkServer?.Dispose();
            _signalkServer = null;

            ////_signalKClient?.Dispose();
            ////_signalKClientParser?.Dispose();
            ////_signalKClient = null;
            ////_signalKClientParser = null;

            _autopilot?.Dispose();
            _autopilot = null;

            _parserHandheldInterface?.Dispose();
            _parserHandheldInterface = null;
        }

        public void SendTemperature(Temperature value)
        {
            _lastTemperature = value;
            // Send with two known types
            TransducerDataSet ds = new TransducerDataSet("C", value.DegreesCelsius, "C", "ENV_OUTAIR_T");
            var msg = new TransducerMeasurement(new[] { ds });
            _router.SendSentence(msg);
            ds = new TransducerDataSet("C", value.DegreesCelsius, "C", "Air");
            msg = new TransducerMeasurement(new[] { ds });
            _router.SendSentence(msg);
        }

        public void SendPressure(Pressure value)
        {
            TransducerDataSet ds = new TransducerDataSet("P", value.Hectopascals / 1000.0, "B", "Barometer");
            var msg = new TransducerMeasurement(new[] { ds });
            _router.SendSentence(msg);

            if (_lastTemperature.HasValue && _lastHumidity.HasValue)
            {
                // MDA sentence is actually obsolete, but it may still be recognized by more hardware than the XDR sentences
                Temperature dewPoint = WeatherHelper.CalculateDewPoint(_lastTemperature.Value, _lastHumidity.Value);
                MeteorologicalComposite ms =
                    new MeteorologicalComposite(value, _lastTemperature, null, _lastHumidity, dewPoint);
                _router.SendSentence(ms);
            }
        }

        public void SendHumidity(RelativeHumidity value)
        {
            TransducerDataSet ds = new TransducerDataSet("H", value.Percent, "P", "ENV_INSIDE_H");
            var msg = new TransducerMeasurement(new[] { ds });
            _router.SendSentence(msg);
            _lastHumidity = value;
        }

        public void Send(NmeaSentence sentence)
        {
            _router.SendSentence(sentence);
        }

        /// <summary>
        /// Sends engine specific data (this uses the so-called SeaSmart.Net protocol, actually a kind of
        /// NMEA-2000 messages wrapped in NMEA0183, which my YDNG-03 can convert to real NMEA-2000.
        /// </summary>
        public void SendEngineData(EngineData engineData)
        {
            if (_router == null)
            {
                return; // cleanup in progress
            }

            // This is the old-stlye RPM message. Can carry only a limited set of information and is often no longer
            // recognized by NMEA-2000 displays or converters.
            EngineRevolutions rv = new EngineRevolutions(RotationSource.Engine, engineData.Revolutions, engineData.EngineNo + 1, engineData.Pitch);
            _router.SendSentence(rv);

            // Example data set: (bad example from the docs, since the engine is just not running here)
            // $PCDIN,01F200,000C7A4F,02,000000FFFF7FFFFF*21
            int rpm = (int)engineData.Revolutions.RevolutionsPerMinute;
            rpm = rpm / 64; // Some trying shows that the last 6 bits are shifted out
            if (rpm > short.MaxValue)
            {
                rpm = short.MaxValue;
            }

            string engineNoText = engineData.EngineNo.ToString("X2", CultureInfo.InvariantCulture);
            string rpmText = rpm.ToString("X4", CultureInfo.InvariantCulture);
            int pitchPercent = (int)engineData.Pitch.Percent;
            string pitchText = pitchPercent.ToString("X2", CultureInfo.InvariantCulture);
            string timeStampText = Environment.TickCount.ToString("X8", CultureInfo.InvariantCulture);
            var rs = new RawSentence(new TalkerId('P', 'C'), new SentenceId("DIN"), new string[]
            {
                "01F200",
                timeStampText,
                "02",
                engineNoText + rpmText + "FFFF" /*Boost*/ + pitchText + "FFFF"
            }, DateTimeOffset.UtcNow);
            _router.SendSentence(rs);

            // $PCDIN,01F201,000C7E1B,02,000000FFFF407F0005000000000000FFFF000000000000007F7F*24
            //                           1-2---3---4---5---6---7-------8---9---1011--12--1314
            // 1) Engine no. 0: Cntr/Single
            // 2) Oil pressure
            // 3) Oil temp
            // 4) Engine Temp
            // 5) Alternator voltage
            // 6) Fuel rate
            // 7) Engine operating time (seconds)
            // 8) Coolant pressure
            // 9) Fuel pressure
            // 10) Reserved
            // 11) Status
            // 12) Status
            // 13) Load percent
            // 14) Torque percent
            int operatingTimeSeconds = (int)engineData.OperatingTime.TotalSeconds;
            string operatingTimeString = operatingTimeSeconds.ToString("X8", CultureInfo.InvariantCulture);
            // For whatever reason, this expects this as little endian (all the other way round)
            string swappedString = operatingTimeString.Substring(6, 2) + operatingTimeString.Substring(4, 2) +
                                   operatingTimeString.Substring(2, 2) + operatingTimeString.Substring(0, 2);

            // Status = 0 is ok, anything else seems to indicate a fault
            int status = rpm != 0 ? 0 : 1;
            string statusString = status.ToString("X4", CultureInfo.InvariantCulture);
            int engineTempKelvin = (int)(engineData.EngineTemperature.Kelvins * 100.0);
            string engineTempString = engineTempKelvin.ToString("X4", CultureInfo.InvariantCulture);
            // Seems to require a little endian conversion as well
            engineTempString = engineTempString.Substring(2, 2) + engineTempString.Substring(0, 2);
            rs = new RawSentence(new TalkerId('P', 'C'), new SentenceId("DIN"), new string[]
            {
                "01F201",
                timeStampText,
                "02",
                engineNoText + "0000FFFF" + engineTempString + "00050000" + swappedString + "FFFF000000" + statusString + "00007F7F"
            }, DateTimeOffset.UtcNow);
            _router.SendSentence(rs);
        }
    }
}

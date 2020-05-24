using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Iot.Device.Common;
using Iot.Device.Nmea0183;
using Iot.Device.Nmea0183.Sentences;
using Iot.Units;
using Units;

namespace DisplayControl
{
    public sealed class NmeaSensor : IDisposable
    {
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

        /// <summary>
        /// Client instance for signal-k (we need to read from here, maybe later we drop the separate server above)
        /// </summary>
        private TcpClient _signalKClient;
        private NmeaParser _signalKClientParser;

        private MessageRouter _router;

        private List<SensorValueSource> _values;
        private PositionValue _position;
        private ObservableValue<double> _speed;
        private ObservableValue<double> _track;
        private ObservableValue<string> _parserMsg;
        private ObservableValue<double> _elevation;
        private ObservableValue<double> _windSpeedRelative;
        private ObservableValue<double> _windDirectionRelative;
        private ObservableValue<double> _windSpeedAbsolute;
        private ObservableValue<double> _windDirectionAbsolute;
        private ObservableValue<double> _magneticVariationField;
        private Stream _streamShip;
        private SerialPort _serialPortShip;
        private Stream _streamHandheld;
        private SerialPort _serialPortHandheld;

        private Angle? _magneticVariation;
        private GlobalPositioningSystemFixData _lastGgaMessage;
        private RecommendedMinimumNavigationInformation _lastRmcMessage;
        private TrackMadeGood _lastVtgMessage;
        private WindSpeedAndAngle _lastMwvMessage;
        private Temperature? _lastTemperature;
        private double? _lastHumidity;

        public NmeaSensor()
        {
            _values = new List<SensorValueSource>();
            _magneticVariation = null;
        }

        public List<SensorValueSource> SensorValueSources
        {
            get
            {
                return _values;
            }
        }

        public IList<FilterRule> ConstructRules()
        {
            TalkerId yd = new TalkerId('Y', 'D');
            // Note: Order is important. First ones are checked first
            IList<FilterRule> rules = new List<FilterRule>();
            // Drop any incoming YD sentences from this source, they were processed already
            rules.Add(new FilterRule(SignalKIn, yd, SentenceId.Any, new List<string>(), false ));
            // Log just everything, but of course continue processing
            rules.Add(new FilterRule("*", TalkerId.Any, SentenceId.Any, new []{ MessageRouter.LoggingSinkName }, false, true));
            // GGA messages from the ship are discarded (the ones from the handheld shall be used instead)
            rules.Add(new FilterRule("*", yd, new SentenceId("GGA"), new List<string>(), false, false));
            rules.Add(new FilterRule("*", yd, new SentenceId("RMC"), new List<string>(), false, false));
            // Anything from the local software (i.e. IMU data, temperature data) is sent to the ship and other nav software
            rules.Add(new FilterRule(MessageRouter.LocalMessageSource, TalkerId.Any, SentenceId.Any, new[] { ShipSourceName, OpenCpn, SignalKOut }, false));

            // Anything from SignalK is currently discarded (maybe there are some computed sentences that are useful)
            rules.Add(new FilterRule(SignalKOut, TalkerId.Any, SentenceId.Any, new List<string>()));
            // Anything from OpenCpn is distributed everywhere
            rules.Add(new FilterRule(OpenCpn, TalkerId.Any, SentenceId.Any, new [] { SignalKOut, ShipSourceName, HandheldSourceName }));
            // Anything from the ship is sent locally
            rules.Add(new FilterRule(ShipSourceName, TalkerId.Any, SentenceId.Any, new [] { OpenCpn, SignalKOut, MessageRouter.LocalMessageSource }, false));
            // Anything from the handheld is sent to our processor
            rules.Add(new FilterRule(HandheldSourceName, TalkerId.Any, SentenceId.Any, new[] { MessageRouter.LocalMessageSource }, false, true));

            // ... but excluding the AutoPilot sentences and only as raw to the ship (todo: Analyze)
            string[] gpsSequences = new string[]
            {
                "GGA", "GLL", "RMC", "ZDA", "GSV", "VTG"
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
                rules.Add(new FilterRule(HandheldSourceName, TalkerId.Any, new SentenceId(navigationSentence), new[] { SignalKOut }, true, true));
                // And send the result of that nav operation to the ship 
                // TODO: Choose which nav solution to use: Handheld direct, OpenCPN, signalK, depending on who is ready to do so
                rules.Add(new FilterRule(SignalKIn, new TalkerId('I', 'I'), SentenceId.Any, new []{ ShipSourceName }, true, true));
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
                // TODO: needs testing (we must make sure the autopilot gets sentences only from one nav device, or 
                // it will get confused)
                // Maybe we need to be able to switch between using OpenCpn and the Handheld for autopilot / navigation control
                rules.Add(new FilterRule("*", TalkerId.Any, new SentenceId(autopilot), new []{ HandheldSourceName }, true));
            }
            
            return rules;
        }

        public void Initialize()
        {
            _position = new PositionValue("Position");
            _speed = new ObservableValue<double>("SOG", "kts", 0);
            _track = new ObservableValue<double>("Track", "°", 0);
            _elevation = new ObservableValue<double>("Höhe", "m", 0);
            _parserMsg = new ObservableValue<string>("Nmea parser msg", string.Empty, "Ok");
            _parserMsg.SuppressWarnings = true; // Do many intermittent errors
            _windSpeedRelative = new ObservableValue<double>("Scheinbarer Wind", "kts");
            _windSpeedRelative.ValueFormatter = "F1";
            _windSpeedAbsolute = new ObservableValue<double>("Wahrer Wind", "kts");
            _windSpeedAbsolute.ValueFormatter = "F1";
            _windDirectionAbsolute = new ObservableValue<double>("Scheinbare Windrichtung", "°T");
            _windDirectionRelative = new ObservableValue<double>("Wahre Windrichtung", "°T");
            _magneticVariationField = new ObservableValue<double>("Deklination", "°E");
            
            SensorValueSources.AddRange(new SensorValueSource[]
            {
                _windSpeedRelative, _windDirectionRelative, _windSpeedAbsolute, _windDirectionAbsolute,
                _speed, _track, _parserMsg, _elevation, _position
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

            _signalKClient = new TcpClient("127.0.0.1", 10110);
            _signalKClientParser = new NmeaParser(SignalKIn, _signalKClient.GetStream(), _signalKClient.GetStream());
            _signalKClientParser.OnParserError += OnParserError;
            // _signalKClientParser.ExclusiveTalkerId = new TalkerId('I', 'I');
            _signalKClientParser.StartDecode();

            _router = new MessageRouter(new LoggingConfiguration() { Path = "/home/pi/projects/", MaxFileSize = 1024 * 1024 * 5 });
            _router.AddEndPoint(_parserShipInterface);
            _router.AddEndPoint(_parserHandheldInterface);
            _router.AddEndPoint(_openCpnServer);
            _router.AddEndPoint(_signalkServer);
            _router.AddEndPoint(_signalKClientParser);

            _router.OnNewSequence += ParserOnNewSequence;
            foreach (var rule in ConstructRules())
            {
                _router.AddFilterRule(rule);
            }

            _router.StartDecode();

        }

        private void ParserOnNewSequence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            if (sentence is GlobalPositioningSystemFixData gga && gga.Valid)
            {
                if (_lastGgaMessage != null && _lastGgaMessage.Age < TimeSpan.FromSeconds(1))
                {
                    return;
                }
                _lastGgaMessage = gga;
                _position.Value = gga.Position;
                _elevation.Value = gga.GeoidAltitude.GetValueOrDefault(0);
            }

            if (sentence is RecommendedMinimumNavigationInformation rmc)
            {
                if (_lastRmcMessage != null && _lastRmcMessage.Age < TimeSpan.FromSeconds(1))
                {
                    return;
                }
                _lastRmcMessage = rmc;
                _speed.Value = rmc.Speed.Knots;
                _track.Value = rmc.TrackMadeGoodInDegreesTrue.GetValueOrDefault(Angle.Zero).Degrees;
                _magneticVariation = rmc.MagneticVariationInDegrees;
                if (_magneticVariation.HasValue)
                {
                    _magneticVariationField.Value = _magneticVariation.Value.Degrees;
                }
            }

            if (sentence is TrackMadeGood vtg)
            {
                _lastVtgMessage = vtg;
                _speed.Value = vtg.Speed.Knots;
                _track.Value = vtg.CourseOverGroundTrue.Degrees;
            }

            if (sentence is WindSpeedAndAngle mwv)
            {
                _lastMwvMessage = mwv;
                if (mwv.Relative)
                {
                    _windSpeedRelative.Value = mwv.Speed.Knots;
                    _windDirectionRelative.Value = mwv.Angle.Degrees;
                }
                else
                {
                    _windSpeedAbsolute.Value = mwv.Speed.Knots;
                    _windDirectionAbsolute.Value = mwv.Angle.Degrees;
                }
            }

            // Reset warning if we get a valid message again (TODO: Improve condition)
            _parserMsg.WarningLevel = WarningLevel.None;
        }

        private void OnParserError(NmeaSinkAndSource source, string error, NmeaError errorCode)
        {
            _parserMsg.Value = error;
            if (string.IsNullOrWhiteSpace(error))
            {
                _parserMsg.WarningLevel = WarningLevel.None;
            }
            else
            {
                _parserMsg.WarningLevel = WarningLevel.Warning;
            }

            Console.WriteLine($"Nmea error from {source.InterfaceName}: {error}");
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
                Angle.FromDegrees(value.Z));
            _router.SendSentence(attitude);
        }

        public void Dispose()
        {
            _router?.Dispose();
            _router = null;

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

            _signalKClient?.Dispose();
            _signalKClientParser?.Dispose();
            _signalKClient = null;
            _signalKClientParser = null;

            _parserHandheldInterface?.Dispose();
            _parserHandheldInterface = null;
        }

        public void SendTemperature(Temperature value)
        {
            _lastTemperature = value;
            // Send with two known types
            TransducerDataSet ds = new TransducerDataSet("C", value.Celsius, "C", "ENV_OUTAIR_T");
            var msg = new TransducerMeasurement(new[] { ds });
            _router.SendSentence(msg);
            ds = new TransducerDataSet("C", value.Celsius, "C", "Air");
            msg = new TransducerMeasurement(new[] { ds });
            _router.SendSentence(msg);
        }

        public void SendPressure(Pressure value)
        {
            TransducerDataSet ds = new TransducerDataSet("P", value.Hectopascal / 1000.0, "B", "Barometer");
            var msg = new TransducerMeasurement(new[] { ds });
            _router.SendSentence(msg);

            if (_lastTemperature.HasValue && _lastHumidity.HasValue)
            {
                // MDA sentence is actually obsolete, but it may still be recognized by more hardware than the XDR sentences
                Temperature dewPoint = WeatherHelper.CalculateDewPoint(_lastTemperature.Value, _lastHumidity.Value);
                RawSentence rs = new RawSentence(new TalkerId('E', 'C'), new SentenceId("MDA"),
                    new string[]
                    {
                        value.InchOfMercury.ToString("F2", CultureInfo.InvariantCulture), "I",
                        (value.Hectopascal / 1000).ToString("F3", CultureInfo.InvariantCulture), "B", 
                        _lastTemperature.Value.Celsius.ToString("F1", CultureInfo.InvariantCulture), "C", // air temp
                        "", "C", // Water temp
                        _lastHumidity.Value.ToString("F1", CultureInfo.InvariantCulture), "", // Relative and absolute humidity 
                        dewPoint.Celsius.ToString("F1", CultureInfo.InvariantCulture), "C", // dew point
                        "", "T", "", "M", "", "N", "", "M" // Wind speed, direction (not given here, since would be a round-trip)
                    }, DateTimeOffset.UtcNow);
                _router.SendSentence(rs);
            }
        }

        public void SendHumidity(double value)
        {
            TransducerDataSet ds = new TransducerDataSet("H", value, "P", "ENV_INSIDE_H");
            var msg = new TransducerMeasurement(new[] { ds });
            _router.SendSentence(msg);
            _lastHumidity = value;
        }
    }
}

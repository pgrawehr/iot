using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
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

        private TcpClient _client;

        /// <summary>
        /// This connects to the network server (which is connected to the Ship's network via the Yacht-Devices interface)
        /// </summary>
        private NmeaParser _parserNetworkInterface;

        /// <summary>
        /// This connects to the handheld GPS (input) and the Autopilot (output)
        /// </summary>
        private NmeaParser _parserHandheldInterface;

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
        private Stream _stream;
        private SerialPort _serialPort;
        private Angle? _magneticVariation;

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
            // Note: Order is important. First ones are checked first
            IList<FilterRule> rules = new List<FilterRule>();
            // Anything from the local software (i.e. IMU data, temperature data) is sent to the ship and other nav software
            rules.Add(new FilterRule(MessageRouter.LocalMessageSource, TalkerId.Any, SentenceId.Any, StandardFilterAction.ForwardToPrimary, false));
            // GGA messages from the ship are discarded (the ones from the handheld shall be used instead)
            rules.Add(new FilterRule("*", new TalkerId('Y', 'D'), new SentenceId("GGA"), StandardFilterAction.DiscardMessage));
            // Anything from the ship is sent locally
            rules.Add(new FilterRule(ShipSourceName, TalkerId.Any, SentenceId.Any, StandardFilterAction.ForwardToLocal, false));
            // Anything from the handheld is sent everywhere
            rules.Add(new FilterRule(HandheldSourceName, TalkerId.Any, SentenceId.Any, StandardFilterAction.ForwardToLocal, false));
            // ... but only as raw to the ship
            rules.Add(new FilterRule(HandheldSourceName, TalkerId.Any, SentenceId.Any, StandardFilterAction.ForwardToPrimary, true));
            // Send the autopilot anything he can use, but only from the ship (we need special filters to send him info from ourselves, if there are any)
            string[] autoPilotSentences = new string[]
            {
                "APB", "APA", "RMB", "XTE", "XTR",
                "BPI", "BWR", "BWC",
                "BER", "BEC", "WDR", "WDC", "BOD", "WCV", "VWR", "VHW"
            };
            foreach (var autopilot in autoPilotSentences)
            {
                // TODO: Commented out for now, needs testing (we must make sure the autopilot gets sentences only from one nav device, or 
                // it will get confused)
                // rules.Add(new FilterRule(HandheldSourceName, TalkerId.Any, new SentenceId(autopilot), StandardFilterAction.ForwardToSecondary, true));
                // Send back (but physically different client)
                rules.Add(new FilterRule(ShipSourceName, TalkerId.Any, new SentenceId(autopilot), StandardFilterAction.ForwardToSecondary, true));
            }
            
            return rules;
        }

        public void Initialize()
        {
            _position = new PositionValue("Position");
            _speed = new ObservableValue<double>("SOG", "kts", 0);
            _track = new ObservableValue<double>("Track", "°", 0);
            _elevation = new ObservableValue<double>("Höhe", "m", 0);
            _parserMsg = new ObservableValue<string>("Nmea parser msg", string.Empty, string.Empty);
            _windSpeedRelative = new ObservableValue<double>("Scheinbarer Wind", "kts");
            _windSpeedAbsolute = new ObservableValue<double>("Wahrer Wind", "kts");
            _windDirectionAbsolute = new ObservableValue<double>("Scheinbare Windrichtung", "°T");
            _windDirectionRelative = new ObservableValue<double>("Wahre Windrichtung", "°T");
            _magneticVariationField = new ObservableValue<double>("Deklination", "°E");
            _client = new TcpClient("127.0.0.1", 10110);
            SensorValueSources.AddRange(new SensorValueSource[]
            {
                _windSpeedRelative, _windDirectionRelative, _windSpeedAbsolute, _windDirectionAbsolute,
                _speed, _track, _parserMsg, _elevation, _position
            });

            _stream = _client.GetStream();
            _parserNetworkInterface = new NmeaParser(_stream, _stream);
            _parserNetworkInterface.OnParserError += OnParserError;
            _parserNetworkInterface.StartDecode();

            _serialPort = new SerialPort("/dev/ttyAMA2", 4800);
            _serialPort.Open();

            _parserHandheldInterface = new NmeaParser(_serialPort.BaseStream, _serialPort.BaseStream);
            _parserHandheldInterface.OnParserError += OnParserError;
            _parserHandheldInterface.StartDecode();

            _router = new MessageRouter();
            _router.AddEndPoint(ShipSourceName, _parserNetworkInterface);
            _router.AddEndPoint(HandheldSourceName, _parserHandheldInterface);
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
                _position.Value = gga.Position;
                _elevation.Value = gga.GeoidAltitude.GetValueOrDefault(0);
            }

            if (sentence is RecommendedMinimumNavigationInformation rmc)
            {
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
                _speed.Value = vtg.Speed.Knots;
                _track.Value = vtg.CourseOverGroundTrue.Degrees;
            }

            if (sentence is WindSpeedAndAngle mwv)
            {
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

        private void OnParserError(string error, NmeaError errorCode)
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
        }

        public void SendImuData(Vector3 value)
        {
            // Z is pitch, Y is roll, X is heading
            if (_parserNetworkInterface == null)
            {
                return;
            }
            HeadingMagnetic mag = new HeadingMagnetic(value.X);
            _router.SendSentence(mag);

            if (_magneticVariation != null)
            {
                HeadingTrue hdt = new HeadingTrue(value.X + _magneticVariation.Value.Degrees);
                _router.SendSentence(hdt);
            }

            var attitude = TransducerMeasurement.FromRollAndPitch(Angle.FromDegrees(value.Y),
                Angle.FromDegrees(value.Z));
            _router.SendSentence(attitude);
        }

        public void Dispose()
        {
            _router?.Dispose();
            _router = null;

            if (_client != null)
            {
                _client.Close();
                _client.Dispose();
                _stream.Dispose();
            }

            if (_serialPort != null)
            {
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }

            _parserNetworkInterface?.Dispose();
            _parserNetworkInterface = null;

            _parserHandheldInterface?.Dispose();
            _parserHandheldInterface = null;

            _client = null;
            _stream = null;
        }

        public void SendTemperature(Temperature value)
        {
            TransducerDataSet ds = new TransducerDataSet("C", value.Celsius, "C", "ENV_OUTAIR_T");
            var msg = new TransducerMeasurement(new[] { ds });
            _router.SendSentence(msg);
        }

        public void SendPressure(Pressure value)
        {
            TransducerDataSet ds = new TransducerDataSet("P", value.Hectopascal / 1000.0, "B", "Barometer");
            var msg = new TransducerMeasurement(new[] { ds });
            _router.SendSentence(msg);
        }

        public void SendHumidity(double value)
        {
            TransducerDataSet ds = new TransducerDataSet("H", value, "P", "ENV_INSIDE_H");
            var msg = new TransducerMeasurement(new[] { ds });
            _router.SendSentence(msg);
        }
    }
}

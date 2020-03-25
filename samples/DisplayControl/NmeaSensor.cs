using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Iot.Device.Nmea0183;
using Nmea0183;
using Nmea0183.Sentences;
using Units;

namespace DisplayControl
{
    public sealed class NmeaSensor : IDisposable
    {
        private TcpClient _client;
        private NmeaParser _parser;
        private List<SensorValueSource> _values;
        private PositionValue _position;
        private ObservableValue<double> _speed;
        private ObservableValue<double> _track;
        private ObservableValue<string> _parserMsg;
        private Stream _stream;

        public NmeaSensor()
        {
            _values = new List<SensorValueSource>();
        }

        public List<SensorValueSource> SensorValueSources
        {
            get
            {
                return _values;
            }
        }

        public void Initialize()
        {
            _position = new PositionValue("Position");
            _speed = new ObservableValue<double>("Speed", "kts", 0);
            _track = new ObservableValue<double>("Track", "°", 0);
            _parserMsg = new ObservableValue<string>("Nmea parser msg", string.Empty, string.Empty);
            _client = new TcpClient("127.0.0.1", 10110);
            SensorValueSources.Add(_position);
            SensorValueSources.Add(_speed);
            SensorValueSources.Add(_track);
            SensorValueSources.Add(_parserMsg);
            _stream = _client.GetStream();
            _parser = new NmeaParser(_stream, _stream);
            _parser.OnNewPosition += OnNewPosition;
            _parser.OnParserError += OnParserError;
            _parser.StartDecode();
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

        private void OnNewPosition(IGeographicPosition position, double track, Speed speed)
        {
            _position.Value = new GeographicPosition(position);
            _speed.Value = speed.Knots;
            _track.Value = track;
        }

        public void SendMagneticHeading(SensorValueSource value)
        {
            if (value.ValueDescription != ImuSensor.ShipMagneticHeading)
            {
                throw new InvalidOperationException("This operation should send magnetic heading values only");
            }
            
            HeadingMagnetic mag = new HeadingMagnetic((double)value.GenericValue);
            TalkerSentence ts = new TalkerSentence(Iot.Device.Nmea0183.TalkerId.ElectronicPositioningSystem, mag);
            string dataToSend = ts.ToString() + "\r\n";
            byte[] buffer = Encoding.ASCII.GetBytes(dataToSend);
            _stream.Write(buffer);
        }

        public void Dispose()
        {
            _parser?.Dispose();
            _parser = null;
            if (_client != null)
            {
                _client.Close();
                _client.Dispose();
                _stream.Dispose();
            }

            _client = null;
            _stream = null;
        }
    }
}

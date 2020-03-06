using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Nmea0183;
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
            var stream = _client.GetStream();
            _parser = new NmeaParser(stream, stream);
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

        public void Dispose()
        {
            _parser?.Dispose();
            _parser = null;
            if (_client != null)
            {
                _client.Close();
                _client.Dispose();
            }

            _client = null;
        }
    }
}

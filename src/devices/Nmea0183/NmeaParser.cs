using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Iot.Device.Nmea0183;
using Iot.Device.Nmea0183.Sentences;
using Nmea0183.Sentences;
using Units;

#pragma warning disable CS1591
namespace Nmea0183
{
    /// <summary>
    /// Parses Nmea Sequences
    /// </summary>
    public sealed class NmeaParser : IDisposable
    {
        private readonly Stream _dataSource;
        private readonly Stream _dataSink;
        private readonly object _lock;
        private Thread _parserThread;
        private CancellationTokenSource _cancellationTokenSource;
        private StreamReader _reader;
        private Dictionary<SentenceId, TalkerSentence> _lastSeenSentences;

        /// <summary>
        /// Creates a new instance of the NmeaParser, taking an input and an output stream
        /// </summary>
        /// <param name="dataSource">Data source (may be connected to a serial port, a network interface, or whatever). It is recommended to use a blocking Stream,
        /// to prevent unnecessary polling</param>
        /// <param name="dataSink">Optional data sink, to send information. Can be null, and can be identical to the source stream</param>
        public NmeaParser(Stream dataSource, Stream dataSink)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _reader = new StreamReader(_dataSource); // Nmea sentences are text
            _dataSink = dataSink;
            _lock = new object();
            _lastSeenSentences = new Dictionary<SentenceId, TalkerSentence>();
        }

        public event Action<IGeographicPosition, double, Speed> OnNewPosition;

        public event Action<DateTimeOffset> OnNewTime;

        public event Action<TalkerSentence> OnNewSequence;

        public event Action<string, NmeaError> OnParserError;

        public void StartDecode()
        {
            lock (_lock)
            {
                if (_parserThread != null && _parserThread.IsAlive)
                {
                    throw new InvalidOperationException("Parser thread already started");
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _parserThread = new Thread(Parser);
                _parserThread.Start();
            }
        }

        private void Parser()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                string currentLine = _reader.ReadLine();
                Console.WriteLine(currentLine);
                TalkerSentence sentence = TalkerSentence.FromSentenceString(currentLine, out var error);
                if (sentence == null)
                {
                    Console.WriteLine($"Malformed sentence detected: {currentLine}: {error}");
                    OnParserError?.Invoke(currentLine, error);
                    continue;
                }

                OnNewSequence?.Invoke(sentence);
                _lastSeenSentences[sentence.Id] = sentence;

                var typed = sentence.TryGetTypedValue();
                if (typed is RecommendedMinimumNavigationInformation rmc)
                {
                    // Todo: This sentence is only interesting if we don't have GGA and VTG
                    if (rmc.LatitudeDegrees.HasValue && rmc.LongitudeDegrees.HasValue)
                    {
                        GeographicPosition position = new GeographicPosition(rmc.LatitudeDegrees.Value, rmc.LongitudeDegrees.Value, 0);
                        Console.WriteLine($"Your location: {position}");

                        if (rmc.TrackMadeGoodInDegreesTrue.HasValue && rmc.SpeedOverGroundInKnots.HasValue)
                        {
                            OnNewPosition?.Invoke(position, rmc.TrackMadeGoodInDegreesTrue.Value, Speed.FromKnots(rmc.SpeedOverGroundInKnots.Value));
                        }
                        else
                        {
                            OnNewPosition?.Invoke(position, 0, Speed.FromKnots(0));
                        }
                    }
                }
                else if (typed is TimeDate td)
                {
                    if (td.Valid && td.DateTime.HasValue)
                    {
                        Console.WriteLine($"Current time: {td.DateTime.Value}");
                        OnNewTime?.Invoke(td.DateTime.Value);
                    }
                }
                else
                {
                    Console.WriteLine($"Sentence of type `${sentence.TalkerId}{sentence.Id}` not handled.");
                }
            }
        }

        public void StopDecode()
        {
            lock (_lock)
            {
                if (_parserThread != null && _parserThread.IsAlive && _cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _parserThread.Join();
                    _cancellationTokenSource = null;
                    _parserThread = null;
                }
            }
        }

        public void Dispose()
        {
            StopDecode();
        }
    }
}

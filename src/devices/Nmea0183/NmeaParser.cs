using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Iot.Device.Nmea0183.Sentences;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    public delegate void PositionUpdate(GeographicPosition position, Angle? track, Speed? speed);

    /// <summary>
    /// Parses Nmea Sequences
    /// </summary>
    public class NmeaParser : NmeaSinkAndSource, IDisposable
    {
        private readonly object _lock;
        private Stream _dataSource;
        private Stream _dataSink;
        private Thread _parserThread;
        private CancellationTokenSource _cancellationTokenSource;
        private StreamReader _reader;
        private Raw8BitEncoding _encoding;

        /// <summary>
        /// Creates a new instance of the NmeaParser, taking an input and an output stream
        /// </summary>
        /// <param name="interfaceName">Friendly name of this interface (used for filtering and eventually logging)</param>
        /// <param name="dataSource">Data source (may be connected to a serial port, a network interface, or whatever). It is recommended to use a blocking Stream,
        /// to prevent unnecessary polling</param>
        /// <param name="dataSink">Optional data sink, to send information. Can be null, and can be identical to the source stream</param>
        public NmeaParser(String interfaceName, Stream dataSource, Stream dataSink)
        : base(interfaceName)
        {
            _encoding = new Raw8BitEncoding();
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _reader = new StreamReader(_dataSource, _encoding); // Nmea sentences are text
            _dataSink = dataSink;
            _lock = new object();
            ExclusiveTalkerId = TalkerId.Any;
        }

        /// <summary>
        /// Set this to anything other than <see cref="TalkerId.Any"/> to receive only that specific ID from this parser
        /// </summary>
        public TalkerId ExclusiveTalkerId
        {
            get;
            set;
        }

        public override void StartDecode()
        {
            lock (_lock)
            {
                if (_parserThread != null && _parserThread.IsAlive)
                {
                    throw new InvalidOperationException("Parser thread already started");
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _parserThread = new Thread(Parser);
                _parserThread.Name = $"Nmea Parser for {InterfaceName}";
                _parserThread.Start();
            }
        }

        private void Parser()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                string currentLine = null;
                try
                {
                    currentLine = _reader.ReadLine();
                }
                catch (IOException x)
                {
                    FireOnParserError(x.Message, NmeaError.PortClosed);
                    continue;
                }
                catch (ObjectDisposedException x)
                {
                    FireOnParserError(x.Message, NmeaError.PortClosed);
                    continue;
                }

                if (currentLine == null)
                {
                    try
                    {
                        if (_reader.EndOfStream)
                        {
                            FireOnParserError("End of stream detected.", NmeaError.PortClosed);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Ignore here (already reported above)
                    }

                    Thread.Sleep(10); // to prevent busy-waiting
                    continue; // Probably because the stream was closed.
                }

                // Console.WriteLine(currentLine);
                TalkerSentence sentence = TalkerSentence.FromSentenceString(currentLine, ExclusiveTalkerId, out var error);
                if (sentence == null)
                {
                    // If error is none, but the return value is null, we just ignored that message.
                    if (error != NmeaError.None)
                    {
                        FireOnParserError($"Received invalid sentence {currentLine}: Error {error}.", error);
                    }

                    continue;
                }

                NmeaSentence typed = sentence.TryGetTypedValue();
                DispatchSentenceEvents(typed);

                if (!(typed is RawSentence))
                {
                    // If we didn't dispatch it as raw sentence, do this as well
                    RawSentence raw = sentence.GetAsRawSentence();
                    DispatchSentenceEvents(raw);
                }
            }
        }

        public override void SendSentence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            // Console.WriteLine($"Sending sentence ${sentence.TalkerId}{sentence.SentenceId},{sentence.ToNmeaMessage()} from {source.InterfaceName} to {InterfaceName}");
            TalkerSentence ts = new TalkerSentence(sentence);
            string dataToSend = ts.ToString() + "\r\n";
            byte[] buffer = _encoding.GetBytes(dataToSend);
            try
            {
                _dataSink.Write(buffer);
            }
            catch (ObjectDisposedException)
            {
                // Todo: return false
            }
        }

        public override void StopDecode()
        {
            lock (_lock)
            {
                if (_parserThread != null && _parserThread.IsAlive && _cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _dataSource.Dispose();
                    _dataSink.Dispose();
                    _reader.Dispose();
                    _parserThread.Join();
                    _cancellationTokenSource = null;
                    _parserThread = null;
                    _dataSource = null;
                    _dataSink = null;
                    _reader = null;
                }
            }
        }
    }
}

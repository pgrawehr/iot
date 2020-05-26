using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Iot.Device.Nmea0183.Sentences;
using UnitsNet;

#pragma warning disable CS1591

namespace Iot.Device.Nmea0183
{
    public sealed class LoggingSink : NmeaSinkAndSource
    {
        private object _lock;
        private FileStream _logFile;
        private TextWriter _textWriter;
        private NmeaSentence _lastSentence;

        public LoggingSink(string name, LoggingConfiguration configuration)
        : base(name)
        {
            _logFile = null;
            _lock = new object();
            // So we do not need to do a null test later
            _lastSentence = new CrossTrackError(Length.Zero, true);
            Configuration = configuration ?? new LoggingConfiguration();
        }

        public LoggingConfiguration Configuration
        {
            get;
        }

        public override void StartDecode()
        {
            lock (_lock)
            {
                if (!string.IsNullOrWhiteSpace(Configuration.Path))
                {
                    StartNewFile();
                }
            }
        }

        private void StartNewFile()
        {
            string datePart = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm", CultureInfo.InvariantCulture);
            string fileName = Path.Combine(Configuration.Path, "Nmea-" + datePart + ".txt");
            if (_logFile != null)
            {
                _textWriter.Flush();
                _logFile.Close();
            }

            _logFile = new FileStream(fileName, FileMode.Append, FileAccess.Write);
            _textWriter = new StreamWriter(_logFile);
        }

        public override void SendSentence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            lock (_lock)
            {
                if (_textWriter != null)
                {
                    // If talker and ID are the same, assume it's the same message
                    if (_lastSentence.SentenceId != sentence.SentenceId && _lastSentence.TalkerId != sentence.TalkerId)
                    {
                        string msg = FormattableString.Invariant(
                            $"{sentence.DateTime:s}|{source.InterfaceName}|${sentence.TalkerId}{sentence.SentenceId},{sentence.ToNmeaMessage()}|{sentence.ToReadableContent()}");
                        _textWriter.WriteLine(msg);
                    }

                    if ((_logFile.Length > Configuration.MaxFileSize) && (Configuration.MaxFileSize != 0))
                    {
                        StartNewFile();
                    }

                    _lastSentence = sentence;
                }
            }
        }

        public override void StopDecode()
        {
            lock (_lock)
            {
                if (_logFile != null)
                {
                    _textWriter.Flush();
                    _logFile.Close();
                    _logFile = null;
                    _textWriter = null;
                }
            }
        }
    }
}

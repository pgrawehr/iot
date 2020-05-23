using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Iot.Device.Nmea0183.Sentences;

#pragma warning disable CS1591

namespace Iot.Device.Nmea0183
{
    public sealed class LoggingSink : NmeaSinkAndSource
    {
        private object _lock;
        private FileStream _logFile;
        private TextWriter _textWriter;

        public LoggingSink(string name, LoggingConfiguration configuration)
        : base(name)
        {
            _logFile = null;
            _lock = new object();
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
                    string msg = FormattableString.Invariant($"{sentence.DateTime:s}|{source.InterfaceName}|${sentence.TalkerId}{sentence.SentenceId},{sentence.ToNmeaMessage()}|{sentence.ToReadableContent()}");
                    _textWriter.WriteLine(msg);

                    if ((_logFile.Length > Configuration.MaxFileSize) && (Configuration.MaxFileSize != 0))
                    {
                        StartNewFile();
                    }
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

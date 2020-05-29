using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    public sealed class AutopilotController : IDisposable
    {
        private readonly NmeaSinkAndSource _output;
        private readonly TimeSpan _loopTime = TimeSpan.FromMilliseconds(200);
        private bool _threadRunning;
        private Thread _updateThread;
        private SentenceCache _cache;

        public AutopilotController(SentenceCache sentenceCache, NmeaSinkAndSource output)
        {
            _output = output;
            _cache = sentenceCache;
            _threadRunning = false;
        }

        public void Start()
        {
            if (_threadRunning)
            {
                return;
            }

            _threadRunning = true;
            _updateThread = new Thread(Loop);
            _updateThread.Start();
        }

        private void Loop()
        {
            while (_threadRunning)
            {
                Thread.Sleep(_loopTime);
            }
        }

        public void Dispose()
        {
            if (_updateThread != null)
            {
                _threadRunning = false;
                _updateThread.Join();
                _updateThread = null;
            }

            _cache.Dispose();
        }
    }
}

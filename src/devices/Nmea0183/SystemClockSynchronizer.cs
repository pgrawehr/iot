using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Common;
using Iot.Device.Nmea0183.Sentences;
using Microsoft.Extensions.Logging;

namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// Synchronizes the system clock to the NMEA data stream
    /// </summary>
    public class SystemClockSynchronizer : NmeaSinkAndSource
    {
        private int _numberOfValidMessagesSeen;
        private ILogger _logger;

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        public SystemClockSynchronizer()
            : base("System Clock Synchronizer")
        {
            _numberOfValidMessagesSeen = 0;
            _logger = this.GetCurrentClassLogger();
        }

        /// <inheritdoc />
        public override void StartDecode()
        {
            // Nothing to do, this component is only a sink
        }

        /// <inheritdoc />
        public override void SendSentence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            if (sentence is TimeDate zda)
            {
                if (zda.DateTime.HasValue)
                {
                    // Wait a few seconds, so that we're not looking at messages from the cache
                    _numberOfValidMessagesSeen++;
                    if (_numberOfValidMessagesSeen > 10)
                    {
                        TimeSpan delta = (zda.DateTime.Value.UtcDateTime - DateTime.UtcNow);
                        if (Math.Abs(delta.TotalSeconds) > 10)
                        {
                            // The time message seems valid, but it is off by more than 10 seconds from what the system clock
                            // says. Synchronize.
                            SetTime(zda.DateTime.Value.UtcDateTime);
                        }
                    }
                }
            }
        }

        private void SetTime(DateTime dt)
        {
            try
            {
                _logger.LogInformation($"About to synchronize clock to {dt}");
                SystemRealTimeClock.SetSystemTimeUtc(dt);
            }
            catch (Exception e) when (e is UnauthorizedAccessException || e is IOException)
            {
                _logger.LogError(e, "Unable to set system time");
                return;
            }

            _logger.LogInformation($"Successfully set time. System time is now {DateTime.UtcNow}");
        }

        /// <inheritdoc />
        public override void StopDecode()
        {
        }
    }
}

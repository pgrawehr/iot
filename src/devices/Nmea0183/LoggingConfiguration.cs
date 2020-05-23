using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591

namespace Iot.Device.Nmea0183
{
    public sealed class LoggingConfiguration
    {
        public LoggingConfiguration()
        {
            Path = string.Empty;
        }

        public string Path
        {
            get;
            set;
        }

        public long MaxFileSize
        {
            get;
            set;
        }
    }
}

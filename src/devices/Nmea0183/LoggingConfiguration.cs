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
            Filename = string.Empty;
        }

        public string Filename
        {
            get;
            set;
        }
    }
}

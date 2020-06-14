using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// Configuration settings for NMEA logging
    /// </summary>
    public sealed class LoggingConfiguration
    {
        /// <summary>
        /// Creates a new instance of <see cref="LoggingConfiguration"/>
        /// </summary>
        public LoggingConfiguration()
        {
            Path = string.Empty;
        }

        /// <summary>
        /// Root path of the log file(s)
        /// </summary>
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a new file when this size is reached
        /// </summary>
        public long MaxFileSize
        {
            get;
            set;
        }

        /// <summary>
        /// True to create a sub-folder for each new day
        /// </summary>
        public bool SortByDate
        {
            get;
            set;
        }
    }
}

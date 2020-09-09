using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Common
{
    /// <summary>
    /// Status of the last measurement (Combinations possible)
    /// </summary>
    [Flags]
    public enum SensorMeasurementStatus
    {
        /// <summary>
        /// Everything is fine, the data is good
        /// </summary>
        None = 0,

        /// <summary>
        /// There is no data from this sensor available. Note: Set <see cref="SensorError"/> as well if this is critical
        /// (i.e. after several failed attempts)
        /// </summary>
        NoData = 1,

        /// <summary>
        /// The data is available, but it is close to getting outside of the expected range
        /// </summary>
        Warning = 2,

        /// <summary>
        /// The data is critically high or low or otherwise problematic
        /// </summary>
        Critical = 4,

        /// <summary>
        /// There was an error reading the data. Usually only used after several failed attempts to read a value.
        /// </summary>
        SensorError = 8,

        /// <summary>
        /// The sensor value is indirectly obtained (optional, helpful to determine whether something needs to be
        /// calculated or is available directly)
        /// </summary>
        IndirectResult = 16
    }
}

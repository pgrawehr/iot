using System;
using System.Collections.Generic;
using System.Text;
using UnitsNet;

namespace Iot.Device.Common
{
    /// <summary>
    /// Represents a value together with a time when it was valid. Generally used when handling historic measurements or statistics
    /// </summary>
    public struct HistoricValue
    {
        /// <summary>
        /// Constructs an instance of this structure, with a time and a value.
        /// Note: UTC times preferred.
        /// </summary>
        public HistoricValue(DateTime measurementTime, IQuantity value)
        {
            MeasurementTime = measurementTime;
            Value = value;
        }

        /// <summary>
        /// Time of measurement, UTC
        /// </summary>
        public DateTime MeasurementTime
        {
            get;
        }

        /// <summary>
        /// The value measured
        /// </summary>
        public IQuantity Value
        {
            get;
        }
    }
}

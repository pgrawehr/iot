using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Common
{
    /// <summary>
    /// The kind of medium that was measured. Samples given in braces.
    /// </summary>
    public enum SensorMedium
    {
        /// <summary>
        /// Undefined, unspecified
        /// </summary>
        Undefined,

        /// <summary>
        /// Air (temperature, pressure, humidity)
        /// </summary>
        Air,

        /// <summary>
        /// Water (temperature, speed trough water)
        /// </summary>
        Water,

        /// <summary>
        /// Wind (Speed, Direction)
        /// </summary>
        Wind,

        /// <summary>
        /// Oil (Engine oil temperature or pressure; hydraulic oil pressure)
        /// </summary>
        Oil,

        /// <summary>
        /// Fuel (quantity, typically diesel or gasoline)
        /// </summary>
        Fuel,

        /// <summary>
        /// Freshwater (quantity, percentage)
        /// </summary>
        Freshwater,

        /// <summary>
        /// Waster water tank (percentage full)
        /// </summary>
        WasteWater
    }
}

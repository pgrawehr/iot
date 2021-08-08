using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Common
{
    /// <summary>
    /// A custom data object, but which fulfills the same interface than <see cref="SensorMeasurement"/>
    /// </summary>
    public class CustomData<T> : SensorMeasurement
    {
        /// <summary>
        /// Constructs a custom data instance with a custom quantity
        /// </summary>
        public CustomData(String name, T value, SensorSource source, int instanceNo = 1)
        : base(name, new CustomQuantity<T>(value), source, instanceNo)
        {
        }

        /// <summary>
        /// Constructs a custom data instance with a custom quantity
        /// </summary>
        public CustomData(String name, T value, SensorSource source, int instanceNo, TimeSpan maxMeasurementAge)
            : base(name, new CustomQuantity<T>(value), source, instanceNo, maxMeasurementAge)
        {
        }

        /// <summary>
        /// Update the value
        /// </summary>
        public void UpdateValue(T value)
        {
            UpdateValue(new CustomQuantity<T>(value), false);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (Value == null)
            {
                return string.Empty;
            }

            return Value.ToString()!;
        }
    }
}

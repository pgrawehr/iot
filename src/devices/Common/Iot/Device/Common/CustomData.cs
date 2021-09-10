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
        /// Provides a way of manually specifying the formatting of this value.
        /// Note that this replaces the base class implementation, but is type safe.
        /// </summary>
        public new Func<T, string>? CustomFormatOperation
        {
            get;
            set;
        }

        /// <summary>
        /// Update the value
        /// </summary>
        public void UpdateValue(T value, SensorMeasurementStatus status)
        {
            UpdateValue(new CustomQuantity<T>(value), status, false);
        }

        /// <summary>
        /// Update the value
        /// </summary>
        public void UpdateValue(T value)
        {
            UpdateValue(new CustomQuantity<T>(value), SensorMeasurementStatus.None, false);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            // So this stays thread safe
            var v = Value;
            if (v == null)
            {
                return string.Empty;
            }

            if (CustomFormatOperation != null)
            {
                return CustomFormatOperation(((CustomQuantity<T>)v).Value);
            }

            // This one might still exist. As long as the exact type is not needed for formatting, this will be just fine.
            if (base.CustomFormatOperation != null)
            {
                return base.CustomFormatOperation(v);
            }

            return v.ToString()!;
        }
    }
}

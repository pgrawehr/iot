using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    /// <summary>
    /// Represents a handle to a single kind of measurement.
    /// Examples for concrete instances:
    /// "outside air temperature"
    /// "wind speed"
    /// "water temperature"
    /// "oil pressure"
    /// </summary>
    public class SensorMeasurement : INotifyPropertyChanged
    {
        private SensorLocation _sensorLocation;
        private SensorMedium _sensorMedium;

        private IQuantity _value;

        /// <summary>
        /// Has this ever been fed with a proper value?
        /// </summary>
        /// <remarks>
        /// The reason for this weird flag is that we want to be able to define instances of SensorMeasurement (and add them to
        /// other classes) even before we get actual measurements or before we know whether the sensor is actually operational.
        /// </remarks>
        private bool _hasProperValue;

        /// <summary>
        /// Creates a new instance with a given quantity.
        /// The actual value of the instance is ignored until a first value is set using <see cref="UpdateValue"/>, the argument
        /// is only used to define the physical quantity and the default unit for the values that will be managed.
        /// </summary>
        /// <param name="value">The value definition (quantity, unit)</param>
        public SensorMeasurement(IQuantity value)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            SensorLocation = SensorLocation.Undefined;
            SensorMedium = SensorMedium.Undefined;
            _hasProperValue = false;
        }

        /// <summary>
        /// Creates a new instance with a given quantity.
        /// The actual value of the instance is ignored until a first value is set using <see cref="UpdateValue"/>, the argument
        /// is only used to define the physical quantity and the default unit for the values that will be managed.
        /// </summary>
        /// <param name="value">The value definition (quantity, unit)</param>
        /// <param name="location">Location of the sensor (i.e. inside, outside, engine)</param>
        /// <param name="medium">What is being measured (water, air)</param>
        public SensorMeasurement(IQuantity value, SensorLocation location, SensorMedium medium)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            SensorLocation = location;
            SensorMedium = medium;
            _hasProperValue = false;
        }

        /// <summary>
        /// Where the sensor is located.
        /// </summary>
        public SensorLocation SensorLocation
        {
            get
            {
                return _sensorLocation;
            }
            set
            {
                _sensorLocation = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Sensor medium (what kind of substance is being measured)
        /// </summary>
        public SensorMedium SensorMedium
        {
            get
            {
                return _sensorMedium;
            }
            set
            {
                _sensorMedium = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Retrieves the current value. Use <see cref="UpdateValue"/> to update the value.
        /// This will return null unless an initial value has been defined.
        /// </summary>
        public IQuantity Value
        {
            get
            {
                if (!_hasProperValue)
                {
                    return null;
                }

                return _value;
            }
        }

        public Enum Unit
        {
            get
            {
                return Value.Unit;
            }
        }

        public QuantityType Type
        {
            get
            {
                return Value.Type;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action<SensorMeasurement> ValueChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Updates the value of the sensor.
        /// Only values with the same physical quantity than the current instance already has are accepted.
        /// </summary>
        /// <param name="value">The new value. Pass null to indicate that there's no valid measurement any more (i.e.
        /// the sensor doesn't work for some reason and we want to pass this information to the user instead of keeping
        /// the last good value).</param>
        public void UpdateValue(IQuantity value)
        {
            if (value == null)
            {
                _hasProperValue = false;
                OnPropertyChanged(nameof(Value));
                ValueChanged?.Invoke(this);
                return;
            }

            if (_value.Type != value.Type)
            {
                throw new InvalidOperationException($"This {nameof(SensorMeasurement)} contains {_value.Type}, you cannot change it to {value.Type}.");
            }

            _value = value;
            _hasProperValue = true;
            OnPropertyChanged(nameof(Value));
            ValueChanged?.Invoke(this);
        }
    }
}

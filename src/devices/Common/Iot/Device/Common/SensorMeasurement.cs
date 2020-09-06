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
        private SensorSource _sensorSource;

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
        /// <param name="source">Sensor source identification</param>
        /// <param name="instanceNumber">Sensor instance (if there are multiple sensors of the same kind)</param>
        public SensorMeasurement(IQuantity value, SensorSource source, int instanceNumber = 1)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            Name = source.ToString() + " " + value.Type;
            SensorSource = source;
            Instance = instanceNumber;
            _hasProperValue = false;
        }

        /// <summary>
        /// Creates a new instance with a given quantity.
        /// The actual value of the instance is ignored until a first value is set using <see cref="UpdateValue"/>, the argument
        /// is only used to define the physical quantity and the default unit for the values that will be managed.
        /// </summary>
        /// <param name="name">Name of element (english, probably needs translation outside)</param>
        /// <param name="value">The value definition (quantity, unit)</param>
        /// <param name="source">Sensor source identification</param>
        /// <param name="instanceNumber">Sensor instance (if there are multiple sensors of the same kind)</param>
        public SensorMeasurement(string name, IQuantity value, SensorSource source, int instanceNumber = 1)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            Name = name;
            SensorSource = source;
            Instance = instanceNumber;
            _hasProperValue = false;
        }

        public static SensorMeasurement AirTemperatureOutside = new SensorMeasurement(Temperature.Zero, SensorSource.Air);
        public static SensorMeasurement AirPressureRawOutside = new SensorMeasurement(Pressure.Zero, SensorSource.Air);
        public static SensorMeasurement AirPressureBarometricOutside = new SensorMeasurement(Pressure.Zero, SensorSource.Air, 2);
        public static SensorMeasurement AirHumidityOutside = new SensorMeasurement(Ratio.Zero, SensorSource.Air);
        public static SensorMeasurement AirTemperatureInside = new SensorMeasurement(Temperature.Zero, SensorSource.Air, -1);
        public static SensorMeasurement AirPressureRawInside = new SensorMeasurement(Pressure.Zero, SensorSource.Air, -1);
        public static SensorMeasurement AirPressureBarometricInside = new SensorMeasurement(Pressure.Zero, SensorSource.Air, -2);
        public static SensorMeasurement AirHumidityInside = new SensorMeasurement(Ratio.Zero, SensorSource.Air, -1);

        // Prefer an instance of GeographicPosition, but these make things more compatible to the unit system
        public static SensorMeasurement Latitude = new SensorMeasurement(Angle.Zero, SensorSource.Position, 0);
        public static SensorMeasurement Longitude = new SensorMeasurement(Angle.Zero, SensorSource.Position, 1);
        public static SensorMeasurement Altitude = new SensorMeasurement(Length.Zero, SensorSource.Position, 1);

        /// <summary>
        /// Source of the measurement
        /// </summary>
        public SensorSource SensorSource
        {
            get
            {
                return _sensorSource;
            }
            set
            {
                _sensorSource = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Sensor instance number
        /// </summary>
        public int Instance
        {
            get;
        }

        public string Name
        {
            get;
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

        public T GetAs<T>()
        {
            return Value is T ? (T)Value : default;
        }
    }
}

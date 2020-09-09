using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
    public class SensorMeasurement : INotifyPropertyChanged, IObservable<IQuantity>
    {
        private SensorSource _sensorSource;

        private IQuantity _value;

        private List<IObserver<IQuantity>> _observers;

        /// <summary>
        /// Status of last update
        /// </summary>
        /// <remarks>
        /// Initially set to NoData, since want to be able to define instances of SensorMeasurement (and add them to
        /// other classes) even before we get actual measurements or before we know whether the sensor is actually operational.
        /// </remarks>
        private SensorMeasurementStatus _measurementStatus;

        /// <summary>
        /// Creates a new instance with a given quantity.
        /// The actual value of the instance is ignored until a first value is set using <see cref="UpdateValue(IQuantity)"/>, the argument
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
            _measurementStatus = SensorMeasurementStatus.NoData;
            _observers = new List<IObserver<IQuantity>>();
        }

        /// <summary>
        /// Creates a new instance with a given quantity.
        /// The actual value of the instance is ignored until a first value is set using <see cref="UpdateValue(IQuantity)"/>, the argument
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
            _measurementStatus = SensorMeasurementStatus.NoData;
        }

        public static readonly SensorMeasurement CpuTemperature = new SensorMeasurement("CPU Temperature", Temperature.Zero, Common.SensorSource.Cpu);
        public static readonly SensorMeasurement AirTemperatureOutside = new SensorMeasurement(Temperature.Zero, SensorSource.Air);
        public static readonly SensorMeasurement AirPressureRawOutside = new SensorMeasurement(Pressure.Zero, SensorSource.Air);
        public static readonly SensorMeasurement AirPressureBarometricOutside = new SensorMeasurement(Pressure.Zero, SensorSource.Air, 2);
        public static readonly SensorMeasurement AirHumidityOutside = new SensorMeasurement(Ratio.Zero, SensorSource.Air);
        public static readonly SensorMeasurement AirTemperatureInside = new SensorMeasurement(Temperature.Zero, SensorSource.Air, -1);
        public static readonly SensorMeasurement AirPressureRawInside = new SensorMeasurement(Pressure.Zero, SensorSource.Air, -1);
        public static readonly SensorMeasurement AirHumidityInside = new SensorMeasurement(Ratio.Zero, SensorSource.Air, -1);
        public static readonly SensorMeasurement AirSpeed = new SensorMeasurement("Air Speed", Angle.Zero, SensorSource.Air);
        public static readonly SensorMeasurement WindSpeedApparent = new SensorMeasurement("Apparent Wind Speed", Speed.Zero, SensorSource.WindRelative);
        public static readonly SensorMeasurement WindDirectionApparent = new SensorMeasurement("Apparent Wind Direction", Angle.Zero, SensorSource.WindRelative);
        public static readonly SensorMeasurement WindSpeedTrue = new SensorMeasurement("True Wind Speed", Speed.Zero, SensorSource.WindRelative);

        /// <summary>
        /// True wind direction, ship-relative direction (i.e. 60° from port)
        /// </summary>
        public static readonly SensorMeasurement WindDirectionTrue = new SensorMeasurement("True Wind Direction", Angle.Zero, SensorSource.WindRelative);

        public static readonly SensorMeasurement WindSpeedAbsolute = new SensorMeasurement("Geographic Wind Speed", Speed.Zero, SensorSource.WindRelative);

        /// <summary>
        /// True wind direction, fixed orientation (i.e. 0° = from north)
        /// </summary>
        public static readonly SensorMeasurement WindDirectionAbsolute = new SensorMeasurement("Geographic Wind Direction", Speed.Zero, SensorSource.WindRelative);

        public static readonly SensorMeasurement WaterDepth = new SensorMeasurement("Water depth below surface", Length.Zero, SensorSource.WaterAbsolute);
        public static readonly SensorMeasurement SpeedTroughWater = new SensorMeasurement("Speed trough water", Speed.Zero, SensorSource.WaterRelative);

        // Prefer an instance of GeographicPosition, but these make things more compatible to the unit system
        public static readonly SensorMeasurement Latitude = new SensorMeasurement("Latitude", Angle.Zero, SensorSource.Position, 0);
        public static readonly SensorMeasurement Longitude = new SensorMeasurement("Longitude", Angle.Zero, SensorSource.Position, 1);
        public static readonly SensorMeasurement AltitudeEllipsoid = new SensorMeasurement("Altitude above mean sea level (ellipsoid)", Length.Zero, SensorSource.Position, 1);
        public static readonly SensorMeasurement AltitudeGeoid = new SensorMeasurement("Altitude above mean sea level (geoid)", Length.Zero, SensorSource.Position, 1);

        public static readonly SensorMeasurement HeatIndex = new SensorMeasurement("Heat index", Temperature.Zero, SensorSource.Air, 3);
        public static readonly SensorMeasurement DewPointOutside = new SensorMeasurement("Dew Point", Temperature.Zero, SensorSource.Air, 4);

        public static readonly SensorMeasurement SpeedOverGround = new SensorMeasurement("SOG", Speed.Zero, SensorSource.Position);
        public static readonly SensorMeasurement Track = new SensorMeasurement("Track", Angle.FromDegrees(0), SensorSource.Position);
        public static readonly SensorMeasurement Heading = new SensorMeasurement("Heading", Angle.FromDegrees(0), SensorSource.Compass);

        /// <summary>
        /// Raw heading (no compass deviation correction applied)
        /// </summary>
        public static readonly SensorMeasurement HeadingRaw = new SensorMeasurement("Raw Heading", Angle.FromDegrees(0), SensorSource.Compass);
        public static readonly SensorMeasurement Pitch = new SensorMeasurement("Pitch", Angle.FromDegrees(0), SensorSource.Compass);
        public static readonly SensorMeasurement Roll = new SensorMeasurement("Roll", Angle.FromDegrees(0), SensorSource.Compass);

        // Just a bool actually
        public static readonly SensorMeasurement Engine0On = new SensorMeasurement("Engine 0 operation status", Ratio.Zero, SensorSource.Engine, 0);
        public static readonly SensorMeasurement Engine0Rpm = new SensorMeasurement("Engine 0 RPM", RotationalSpeed.FromRevolutionsPerMinute(0), SensorSource.Engine, 0);
        public static readonly SensorMeasurement Engine0OperatingTime = new SensorMeasurement("Engine 0 operating time", Duration.Zero, SensorSource.Engine, 0);
        public static readonly SensorMeasurement Engine0Temperature = new SensorMeasurement("Engine 0 Temperature", Temperature.Zero, Common.SensorSource.Engine, 0);

        public static readonly SensorMeasurement Engine1On = new SensorMeasurement("Engine 1 operation status", Ratio.Zero, SensorSource.Engine, 1);
        public static readonly SensorMeasurement Engine1Rpm = new SensorMeasurement("Engine 1 RPM", RotationalSpeed.FromRevolutionsPerMinute(0), SensorSource.Engine, 1);
        public static readonly SensorMeasurement Engine1OperatingTime = new SensorMeasurement("Engine 1 operating time", Duration.Zero, SensorSource.Engine, 1);
        public static readonly SensorMeasurement Engine1Temperature = new SensorMeasurement("Engine 1 Temperature", Temperature.Zero, Common.SensorSource.Engine, 1);

        /// <summary>
        /// Magnetic variation is usually computed using the NOAA formulas from the position
        /// </summary>
        public static SensorMeasurement MagneticVariation = new SensorMeasurement("Magnetic Variation", Angle.FromDegrees(0), SensorSource.Position);

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
        /// Retrieves the current value. Use <see cref="UpdateValue(IQuantity)"/> to update the value.
        /// This will return null unless an initial value has been defined.
        /// </summary>
        public IQuantity Value
        {
            get
            {
                if (_measurementStatus.HasFlag(SensorMeasurementStatus.NoData))
                {
                    return null;
                }

                // The member is never null
                return _value;
            }
        }

        public Enum Unit
        {
            get
            {
                return _value.Unit;
            }
        }

        public QuantityType Type
        {
            get
            {
                return _value.Type;
            }
        }

        public SensorMeasurementStatus Status
        {
            get
            {
                return _measurementStatus;
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
            UpdateValue(value, SensorMeasurementStatus.None);
        }

        /// <summary>
        /// Updates the value of the sensor.
        /// Only values with the same physical quantity than the current instance already has are accepted.
        /// </summary>
        /// <param name="value">The new value. Pass null to indicate that there's no valid measurement any more (i.e.
        /// the sensor doesn't work for some reason and we want to pass this information to the user instead of keeping
        /// the last good value).</param>
        /// <param name="status">Status of new measurement. NoData is automatically added if null is passed as <paramref name="value"/>.</param>
        public void UpdateValue(IQuantity value, SensorMeasurementStatus status)
        {
            if (value == null)
            {
                _measurementStatus = SensorMeasurementStatus.NoData | status;
                OnPropertyChanged(nameof(Value));
                ValueChanged?.Invoke(this);
                return;
            }

            if (_value.Type != value.Type)
            {
                throw new InvalidOperationException($"This {nameof(SensorMeasurement)} contains {_value.Type}, you cannot change it to {value.Type}.");
            }

            _value = value;
            _measurementStatus = status;
            OnPropertyChanged(nameof(Value));
            ValueChanged?.Invoke(this);
        }

        public bool TryGetAs<T>(out T convertedValue)
        {
            if (_measurementStatus.HasFlag(SensorMeasurementStatus.NoData))
            {
                convertedValue = default(T);
                return false;
            }

            if (Value is T)
            {
                convertedValue = (T)Value;
                return true;
            }
            else
            {
                convertedValue = default(T);
                return false;
            }
        }

        public override string ToString()
        {
            if (_measurementStatus.HasFlag(SensorMeasurementStatus.NoData))
            {
                return "N/A";
            }

            return Name + ": " + Value.ToString("g", CultureInfo.CurrentCulture);
        }

        public IDisposable Subscribe(IObserver<IQuantity> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            return new Unsubscriber(_observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<IQuantity>> _observers;
            private IObserver<IQuantity> _observer;

            public Unsubscriber(List<IObserver<IQuantity>> observers, IObserver<IQuantity> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                {
                    _observers.Remove(_observer);
                }
            }
        }
    }
}

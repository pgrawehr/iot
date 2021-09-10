using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
        private static readonly TimeSpan DefaultMeasurementMaximumAge = TimeSpan.FromMinutes(1);

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

        private DateTimeOffset _lastUpdate;

        /// <summary>
        /// Creates a new instance with a given quantity.
        /// The actual value of the instance is ignored until a first value is set using <see cref="UpdateValue(IQuantity, bool)"/>, the argument
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
            _sensorSource = source;
            Instance = instanceNumber;
            _measurementStatus = SensorMeasurementStatus.NoData;
            _observers = new List<IObserver<IQuantity>>();
            _lastUpdate = DateTimeOffset.MinValue;
            MaxMeasurementAge = DefaultMeasurementMaximumAge;
        }

        /// <summary>
        /// Creates a new instance with a given quantity.
        /// The actual value of the instance is ignored until a first value is set using <see cref="UpdateValue(IQuantity, bool)"/>, the argument
        /// is only used to define the physical quantity and the default unit for the values that will be managed.
        /// </summary>
        /// <param name="name">Name of element (english, probably needs translation outside)</param>
        /// <param name="value">The value definition (quantity, unit)</param>
        /// <param name="source">Sensor source identification</param>
        /// <param name="instanceNumber">Sensor instance (if there are multiple sensors of the same kind)</param>
        /// <param name="maxMeasurementAge">The maximum age a value is considered valid. Default is 1 Minute</param>
        public SensorMeasurement(string name, IQuantity value, SensorSource source, int instanceNumber, TimeSpan maxMeasurementAge)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            Name = name;
            _sensorSource = source;
            Instance = instanceNumber;
            _measurementStatus = SensorMeasurementStatus.NoData;
            _observers = new List<IObserver<IQuantity>>();
            _lastUpdate = DateTimeOffset.MinValue;
            MaxMeasurementAge = maxMeasurementAge;
        }

        public static readonly SensorMeasurement CpuTemperature = new SensorMeasurement("CPU Temperature", Temperature.Zero, Common.SensorSource.Cpu, 1, TimeSpan.FromSeconds(5));
        public static readonly SensorMeasurement AirTemperatureOutside = new SensorMeasurement("Outside Air temperature", Temperature.Zero, SensorSource.Air);
        public static readonly SensorMeasurement AirPressureRawOutside = new SensorMeasurement("Outside Raw Pressure", Pressure.Zero, SensorSource.Air);
        public static readonly SensorMeasurement AirPressureBarometricOutside = new SensorMeasurement("Outside Barometric Pressure", Pressure.Zero, SensorSource.Air, 2);
        public static readonly SensorMeasurement AirHumidityOutside = new SensorMeasurement("Outside Humidity", RelativeHumidity.Zero, SensorSource.Air);
        public static readonly SensorMeasurement AirTemperatureInside = new SensorMeasurement("Inside Air Temperature", Temperature.Zero, SensorSource.Air, -1);
        public static readonly SensorMeasurement AirPressureRawInside = new SensorMeasurement("Inside Raw Pressure", Pressure.Zero, SensorSource.Air, -1);
        public static readonly SensorMeasurement AirHumidityInside = new SensorMeasurement("Inside Humidity", RelativeHumidity.Zero, SensorSource.Air, -1);
        public static readonly SensorMeasurement AirSpeed = new SensorMeasurement("Air Speed", Angle.Zero, SensorSource.Air, 1, TimeSpan.FromSeconds(5));
        public static readonly SensorMeasurement WindSpeedApparent = new SensorMeasurement("Apparent Wind Speed", Speed.Zero, SensorSource.WindRelative, 1, TimeSpan.FromSeconds(5));
        public static readonly SensorMeasurement WindDirectionApparent = new SensorMeasurement("Apparent Wind Direction", Angle.Zero, SensorSource.WindRelative, 1, TimeSpan.FromSeconds(5));
        public static readonly SensorMeasurement WindSpeedTrue = new SensorMeasurement("True Wind Speed", Speed.Zero, SensorSource.WindRelative, 1, TimeSpan.FromSeconds(10));

        /// <summary>
        /// True wind direction, ship-relative direction (i.e. 60° from port)
        /// </summary>
        public static readonly SensorMeasurement WindDirectionTrue = new SensorMeasurement("True Wind Direction", Angle.Zero, SensorSource.WindRelative, 1, TimeSpan.FromSeconds(10));

        public static readonly SensorMeasurement WindSpeedAbsolute = new SensorMeasurement("Geographic Wind Speed", Speed.Zero, SensorSource.WindRelative, 1, TimeSpan.FromSeconds(10));

        /// <summary>
        /// True wind direction, fixed orientation (i.e. 0° = from north)
        /// </summary>
        public static readonly SensorMeasurement WindDirectionAbsolute = new SensorMeasurement("Geographic Wind Direction", Angle.Zero, SensorSource.WindRelative, 1, TimeSpan.FromSeconds(10));

        public static readonly SensorMeasurement WaterDepth = new SensorMeasurement("Water depth below surface", Length.Zero, SensorSource.WaterAbsolute, 1, TimeSpan.FromSeconds(5));

        public static readonly SensorMeasurement WaterTemperature =
            new SensorMeasurement("Water temperature", Temperature.Zero, SensorSource.WaterAbsolute);
        public static readonly SensorMeasurement SpeedTroughWater = new SensorMeasurement("Speed trough water", Speed.Zero, SensorSource.WaterRelative, 1, TimeSpan.FromSeconds(5));

        // Prefer an instance of GeographicPosition, but these make things more compatible to the unit system
        public static readonly SensorMeasurement Latitude = new SensorMeasurement("Latitude", Angle.Zero, SensorSource.Position, 0, TimeSpan.FromSeconds(5));
        public static readonly SensorMeasurement Longitude = new SensorMeasurement("Longitude", Angle.Zero, SensorSource.Position, 1, TimeSpan.FromSeconds(5));
        public static readonly SensorMeasurement AltitudeEllipsoid = new SensorMeasurement("Altitude above mean sea level (ellipsoid)", Length.Zero, SensorSource.Position, 1, TimeSpan.FromSeconds(5));
        public static readonly SensorMeasurement AltitudeGeoid = new SensorMeasurement("Altitude above mean sea level (geoid)", Length.Zero, SensorSource.Position, 1, TimeSpan.FromSeconds(5));

        public static readonly SensorMeasurement HeatIndex = new SensorMeasurement("Heat index", Temperature.Zero, SensorSource.Air, 3);
        public static readonly SensorMeasurement DewPointOutside = new SensorMeasurement("Dew Point", Temperature.Zero, SensorSource.Air, 4);

        public static readonly SensorMeasurement SpeedOverGround = new SensorMeasurement("SOG", Speed.Zero, SensorSource.Position, 1, TimeSpan.FromSeconds(5));
        public static readonly SensorMeasurement Track = new SensorMeasurement("Track", Angle.FromDegrees(0), SensorSource.Position, 1, TimeSpan.FromSeconds(5));
        public static readonly SensorMeasurement Heading = new SensorMeasurement("Magnetic Heading", Angle.FromDegrees(0), SensorSource.Compass, 1, TimeSpan.FromSeconds(5));

        public static readonly SensorMeasurement Deviation =
            new SensorMeasurement("Deviation", Angle.FromDegrees(0), SensorSource.Compass, 1);

        /// <summary>
        /// Raw heading (no compass deviation correction applied)
        /// </summary>
        public static readonly SensorMeasurement HeadingRaw = new SensorMeasurement("Compass Heading", Angle.FromDegrees(0), SensorSource.Compass, 1, TimeSpan.FromSeconds(5));
        public static readonly SensorMeasurement Pitch = new SensorMeasurement("Pitch", Angle.FromDegrees(0), SensorSource.Compass, 1, TimeSpan.FromSeconds(5));
        public static readonly SensorMeasurement Roll = new SensorMeasurement("Roll", Angle.FromDegrees(0), SensorSource.Compass, 1, TimeSpan.FromSeconds(5));

        // Just a bool actually
        public static readonly SensorMeasurement Engine0On = new CustomData<bool>("Engine 0 operation status", false, SensorSource.Engine);
        public static readonly SensorMeasurement Engine0Rpm = new SensorMeasurement("Engine 0 RPM", RotationalSpeed.FromRevolutionsPerMinute(0), SensorSource.Engine, 0, TimeSpan.FromSeconds(10));
        public static readonly SensorMeasurement Engine0OperatingTime = new SensorMeasurement("Engine 0 operating time", Duration.Zero, SensorSource.Engine, 0);
        public static readonly SensorMeasurement Engine0Temperature = new SensorMeasurement("Engine 0 Temperature", Temperature.Zero, Common.SensorSource.Engine, 0);

        public static readonly SensorMeasurement Engine1On = new CustomData<bool>("Engine 1 operation status", false, SensorSource.Engine);
        public static readonly SensorMeasurement Engine1Rpm = new SensorMeasurement("Engine 1 RPM", RotationalSpeed.FromRevolutionsPerMinute(0), SensorSource.Engine, 1, TimeSpan.FromSeconds(10));
        public static readonly SensorMeasurement Engine1OperatingTime = new SensorMeasurement("Engine 1 operating time", Duration.Zero, SensorSource.Engine, 1);
        public static readonly SensorMeasurement Engine1Temperature = new SensorMeasurement("Engine 1 Temperature", Temperature.Zero, Common.SensorSource.Engine, 1);

        public static readonly SensorMeasurement Windchill = new SensorMeasurement("Windchill temperature", Temperature.Zero, SensorSource.Air);

        public static readonly SensorMeasurement WindForce =
            new SensorMeasurement("Wind pressure", Pressure.Zero, SensorSource.Air);

        public static readonly SensorMeasurement UtcTime =
            new CustomData<DateTime>("UTC Time from GNSS", DateTime.MinValue, SensorSource.Position, 1, TimeSpan.FromSeconds(2))
                { CustomFormatOperation = x => x.ToLongTimeString() };

        public static readonly SensorMeasurement DistanceToNextWaypoint =
            new SensorMeasurement("Distance to next Waypoint", Length.Zero, SensorSource.Navigation);

        public static readonly SensorMeasurement TimeToNextWaypoint =
            new SensorMeasurement("Time to next Waypoint", Duration.Zero, SensorSource.Navigation) { CustomFormatOperation = x => ((Duration)x).ToTimeSpan().ToString() };

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
        /// A custom formatting operation. Takes the value as input and returns a string.
        /// </summary>
        public Func<IQuantity, string>? CustomFormatOperation
        {
            get;
            set;
        }

        /// <summary>
        /// Retrieves the current value. Use <see cref="UpdateValue(IQuantity, bool)"/> to update the value.
        /// This will return null unless an initial value has been defined.
        /// </summary>
        public virtual IQuantity? Value
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

        public TimeSpan MeasurementAge
        {
            get
            {
                if (_lastUpdate == DateTimeOffset.MinValue)
                {
                    return TimeSpan.MaxValue;
                }

                return DateTimeOffset.Now - _lastUpdate;
            }
        }

        public TimeSpan MaxMeasurementAge
        {
            get;
            private set;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event Action<SensorMeasurement, bool>? ValueChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void UpdateStatus()
        {
            if (MeasurementAge > MaxMeasurementAge)
            {
                SensorMeasurementStatus oldStatus = _measurementStatus;
                if ((_measurementStatus & SensorMeasurementStatus.NoData) == SensorMeasurementStatus.None)
                {
                    // did have data before - the sensor is now failing.
                    _measurementStatus |= SensorMeasurementStatus.SensorError;
                }

                _measurementStatus |= SensorMeasurementStatus.NoData;

                if (oldStatus != _measurementStatus)
                {
                    OnPropertyChanged(nameof(Value));
                    OnPropertyChanged(nameof(Status));
                    ValueChanged?.Invoke(this, true);
                }
            }
        }

        /// <summary>
        /// Updates the value of the sensor.
        /// Only values with the same physical quantity than the current instance already has are accepted.
        /// </summary>
        /// <param name="value">The new value. Pass null to indicate that there's no valid measurement any more (i.e.
        /// the sensor doesn't work for some reason and we want to pass this information to the user instead of keeping
        /// the last good value).</param>
        /// <param name="skipAnyMeasurementChanged">True to skip the "AnyMeasurementChanged" callback on the manager</param>
        public void UpdateValue(IQuantity? value, bool skipAnyMeasurementChanged = false)
        {
            UpdateValue(value, SensorMeasurementStatus.None, skipAnyMeasurementChanged);
        }

        /// <summary>
        /// Updates the value of the sensor.
        /// Only values with the same physical quantity than the current instance already has are accepted.
        /// </summary>
        /// <param name="value">The new value. Pass null to indicate that there's no valid measurement any more (i.e.
        /// the sensor doesn't work for some reason and we want to pass this information to the user instead of keeping
        /// the last good value).</param>
        /// <param name="status">Status of new measurement. NoData is automatically added if null is passed as <paramref name="value"/>.</param>
        /// <param name="skipAnyMeasurementChanged">True to skip the "AnyMeasurementChanged" callback on the manager</param>
        public void UpdateValue(IQuantity? value, SensorMeasurementStatus status, bool skipAnyMeasurementChanged)
        {
            _lastUpdate = DateTimeOffset.Now;
            if (value == null)
            {
                SensorMeasurementStatus newStatus = SensorMeasurementStatus.NoData | status;
                if (newStatus != _measurementStatus)
                {
                    _measurementStatus = newStatus;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(Value));
                }

                // Note: Must not skip this callback, even if nothing changed. Some functions (i.e. keeping a history
                // of old values) must get the callback even if the same value is measured multiple times.
                ValueChanged?.Invoke(this, skipAnyMeasurementChanged);
                return;
            }

            if (_value.Type != value.Type)
            {
                throw new InvalidOperationException($"The quantity of '{Name}' is {_value.Type}, you cannot change it to {value.Type}.");
            }

            if (!value.Equals(_value) || status != _measurementStatus)
            {
                _value = value;
                _measurementStatus = status;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Value));
            }

            ValueChanged?.Invoke(this, skipAnyMeasurementChanged);
        }

        public bool TryGetAs<T>(
#if NET5_0_OR_GREATER
            [NotNullWhen(true)]
#endif
            out T convertedValue)
        {
            if (_measurementStatus.HasFlag(SensorMeasurementStatus.NoData))
            {
                convertedValue = default(T)!;
                return false;
            }

            if (Value is T)
            {
                convertedValue = (T)Value;
                return true;
            }
            else
            {
                convertedValue = default(T)!;
                return false;
            }
        }

        public override string ToString()
        {
            if (_measurementStatus.HasFlag(SensorMeasurementStatus.NoData))
            {
                return string.Empty; // Leave to the client to eventually replace with something like "N/A"
            }

            var v = Value;

            if (v == null)
            {
                return String.Empty;
            }

            if (CustomFormatOperation != null)
            {
                return CustomFormatOperation(v);
            }

            return v.ToString("g", CultureInfo.CurrentCulture);
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

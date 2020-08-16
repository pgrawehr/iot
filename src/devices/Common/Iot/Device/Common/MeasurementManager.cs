using System;
using System.Collections.Generic;
using System.Linq;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    public class MeasurementManager
    {
        private readonly List<SensorMeasurement> _measurements;
        private readonly List<MeasurementHistoryConfiguration> _historyConfigurations;
        private readonly object _lock;

        public MeasurementManager()
        {
            _measurements = new List<SensorMeasurement>();
            _historyConfigurations = new List<MeasurementHistoryConfiguration>();
            _lock = new object();
        }

        /// <summary>
        /// Adds a measurement to the manager. No history of this value is being kept, so that only the latest value
        /// will be available.
        /// </summary>
        /// <param name="measurement">The new measurement</param>
        public void AddMeasurement(SensorMeasurement measurement)
        {
            AddMeasurement(measurement, null);
        }

        /// <summary>
        /// Adds a measurement to the manager. A history of old values with the resolution of <paramref name="historyInterval"/> is kept.
        /// Values are kept with the resolution of <paramref name="historyInterval"/> for a maximum of <paramref name="maxMeasurementAge"/>.
        /// Note that if <paramref name="maxMeasurementAge"/> is <code>TimeSpan.MaxValue</code> or another large value, memory usage may
        /// grow unrestricted.
        /// </summary>
        /// <param name="measurement">The measurement to add. Note that this instance is considered a handle and only the value
        /// property will change when a new value is added</param>
        /// <param name="historyInterval">Interval between measurements to keep</param>
        /// <param name="maxMeasurementAge">Maximum age for measurements to be kept in memory</param>
        public void AddMeasurement(SensorMeasurement measurement, TimeSpan historyInterval, TimeSpan maxMeasurementAge)
        {
            var config = new MeasurementHistoryConfiguration(measurement, historyInterval, maxMeasurementAge);
            AddMeasurement(measurement, config);
        }

        public void ConfigureHistory(SensorMeasurement measurement, TimeSpan historyInterval,
            TimeSpan maxMeasurementAge)
        {
            lock (_lock)
            {
                MeasurementHistoryConfiguration existingConfig =
                    _historyConfigurations.FirstOrDefault(x => x.Measurement == measurement);
                if (existingConfig != null)
                {
                    existingConfig.HistoryInterval = historyInterval;
                    existingConfig.MaxMeasurementAge = maxMeasurementAge;
                    existingConfig.RemoveOldEntries();
                }
                else
                {
                    var config = new MeasurementHistoryConfiguration(measurement, historyInterval, maxMeasurementAge);
                    _historyConfigurations.Add(config);
                }
            }
        }

        public void RemoveHistory(SensorMeasurement measurement)
        {
            lock (_lock)
            {
                var entry = _historyConfigurations.FirstOrDefault(x => x.Measurement == measurement);
                _historyConfigurations.Remove(entry);
            }
        }

        private void AddMeasurement(SensorMeasurement measurement, MeasurementHistoryConfiguration historyConfiguration)
        {
            lock (_lock)
            {
                if (measurement == null)
                {
                    throw new ArgumentNullException(nameof(measurement));
                }

                if (_measurements.Contains(measurement))
                {
                    throw new InvalidOperationException("This instance is already registered");
                }

                measurement.ValueChanged += MeasurementOnValueChanged;
                _measurements.Add(measurement);
                if (historyConfiguration != null)
                {
                    _historyConfigurations.Add(historyConfiguration);
                }
            }
        }

        public void RemoveMeasurement(SensorMeasurement measurement)
        {
            lock (_lock)
            {
                measurement.ValueChanged -= MeasurementOnValueChanged;
                _measurements.Remove(measurement);
                RemoveHistory(measurement);
            }
        }

        public IEnumerable<SensorMeasurement> Measurements()
        {
            return new List<SensorMeasurement>(_measurements);
        }

        public IEnumerable<SensorMeasurement> Measurements(Func<SensorMeasurement, bool> predicate)
        {
            return _measurements.Where(predicate);
        }

        public IEnumerable<SensorMeasurement> Measurements(Predicate<SensorMeasurement> predicate)
        {
            return _measurements.Where(x => predicate(x));
        }

        private void MeasurementOnValueChanged(SensorMeasurement measurement)
        {
        }

        private sealed class MeasurementHistoryConfiguration
        {
            private List<(DateTime, IQuantity)> _values;
            private DateTime _lastAddedTime;

            public MeasurementHistoryConfiguration(SensorMeasurement measurement, TimeSpan historyInterval, TimeSpan maxMeasurementAge)
            {
                Measurement = measurement;
                HistoryInterval = historyInterval;
                MaxMeasurementAge = maxMeasurementAge;
                _values = new List<(DateTime, IQuantity)>();
                var now = DateTime.UtcNow;
                _values.Add((now, measurement.Value));
                _lastAddedTime = now;
            }

            public SensorMeasurement Measurement
            {
                get;
            }

            public TimeSpan HistoryInterval
            {
                get;
                set;
            }

            public TimeSpan MaxMeasurementAge
            {
                get;
                set;
            }

            public bool TryAddMeasurement(SensorMeasurement measurement, DateTime utcTimeOfMeasurement)
            {
                var now = utcTimeOfMeasurement;
                if (now > _lastAddedTime + HistoryInterval)
                {
                    _values.Add((now, measurement.Value));
                    _lastAddedTime = now;
                    return true;
                }

                return false;
            }

            public bool TryAddMeasurement(SensorMeasurement measurement)
            {
                return TryAddMeasurement(measurement, DateTime.UtcNow);
            }

            public IList<(DateTime, IQuantity)> Measurements()
            {
                // No clone here, could be expensive!
                return _values;
            }

            public void RemoveOldEntries()
            {
                var oldest = DateTime.UtcNow - MaxMeasurementAge;
                for (int i = 0; i < _values.Count; i++)
                {
                    if (_values[i].Item1 < oldest)
                    {
                        _values.RemoveAt(i);
                        i--;
                    }
                }
            }

            public void Clear()
            {
                _values.Clear();
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    public sealed class MeasurementManager : IDisposable
    {
        private readonly List<SensorMeasurement> _measurements;
        private readonly List<MeasurementHistoryConfiguration> _historyConfigurations;
        private readonly object _lock;

        /// <summary>
        /// True if the callbacks are temporarily disabled to perform a consistent update over multiple values.
        /// Must only be true while the lock is held!
        /// </summary>
        private bool _callbacksBlocked;

        public MeasurementManager()
        {
            _measurements = new List<SensorMeasurement>();
            _historyConfigurations = new List<MeasurementHistoryConfiguration>();
            _lock = new object();
            _callbacksBlocked = false;
        }

        /// <summary>
        /// Triggers when any measurement changes.
        /// Clients should preferably listen to this event rather than <see cref="SensorMeasurement.ValueChanged"/> if they
        /// want to make sure they get consistent data (i.e. from values usually updated by the same source at once, like latitude and longitude)
        /// </summary>
        public event Action<IList<SensorMeasurement>> AnyMeasurementChanged;

        public void Dispose()
        {
            // That's mostly to prevent memory leaks (or more precise, dangling big instances)
            _measurements.Clear();
            _historyConfigurations.Clear();
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

        public void AddRange(IEnumerable<SensorMeasurement> measurements)
        {
            foreach (var m in measurements)
            {
                TryAddMeasurement(m);
            }
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

        public List<HistoricValue> ObtainHistory(SensorMeasurement measurement, TimeSpan maxAge, TimeSpan interval)
        {
            lock (_lock)
            {
                var entry = _historyConfigurations.FirstOrDefault(x => x.Measurement == measurement);
                if (entry == null)
                {
                    throw new InvalidOperationException("No history enabled for this measurement");
                }

                // Pick all entries that are younger than the given age
                List<HistoricValue> ret = new List<HistoricValue>();
                DateTime oldest = DateTime.UtcNow - maxAge;
                foreach (var e in entry.Measurements().Where(x => x.MeasurementTime >= oldest))
                {
                    ret.Add(e);
                }

                if (ret.Count == 0)
                {
                    return ret;
                }

                // Now remove all but the first entry withing each interval.
                // Note that the resulting list may contain gaps in time if the resolution of the history
                // is less thant the expected interval.
                DateTime nextIntervalStart = ret.First().MeasurementTime;
                for (int i = 0; i < ret.Count; i++)
                {
                    // If it's less than where we expect the next value, remove it
                    if (ret[i].MeasurementTime < nextIntervalStart)
                    {
                        ret.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        // otherwise keep the value and move to the next step
                        nextIntervalStart += interval;
                    }
                }

                return ret;
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

        /// <summary>
        /// Adds the measurement if it doesn't exist yet
        /// </summary>
        /// <param name="measurement">New measurement</param>
        /// <returns>True on success, false if it existed already</returns>
        public bool TryAddMeasurement(SensorMeasurement measurement)
        {
            lock (_lock)
            {
                if (_measurements.Contains(measurement))
                {
                    return false;
                }

                AddMeasurement(measurement, null);
                return true;
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

        public List<SensorMeasurement> Measurements()
        {
            return new List<SensorMeasurement>(_measurements);
        }

        public IEnumerable<SensorMeasurement> Measurements(Predicate<SensorMeasurement> predicate)
        {
            return _measurements.Where(x => predicate(x));
        }

        private void MeasurementOnValueChanged(SensorMeasurement measurement)
        {
            lock (_lock)
            {
                var entry = _historyConfigurations.FirstOrDefault(x => x.Measurement == measurement);
                if (entry != null)
                {
                    entry.TryAddMeasurement(measurement.Value);
                    entry.RemoveOldEntries();
                }

                // Done within the lock, so if it is true, we still hold the outer lock and are therefore in
                // the same thread.
                if (!_callbacksBlocked)
                {
                    AnyMeasurementChanged?.Invoke(new[] { measurement });
                }
            }
        }

        public void UpdateValue(SensorMeasurement measurement, IQuantity newValue)
        {
            measurement.UpdateValue(newValue);
        }

        /// <summary>
        /// Updates all the given measurements with new values before triggering an updated event
        /// </summary>
        /// <param name="measurements">List of measurements</param>
        /// <param name="values">List of values (same order as measurements, may contain nulls)</param>
        public void UpdateValues(IList<SensorMeasurement> measurements, IList<IQuantity> values)
        {
            if (measurements.Count != values.Count)
            {
                throw new ArgumentException("Must provide an equal number of measurements and values");
            }

            Monitor.Enter(_lock);
            try
            {
                _callbacksBlocked = true;
                for (int i = 0; i < measurements.Count; i++)
                {
                    measurements[i].UpdateValue(values[i]);
                }

                AnyMeasurementChanged?.Invoke(measurements);
            }
            finally
            {
                _callbacksBlocked = false;
                Monitor.Exit(_lock);
            }
        }

        private sealed class MeasurementHistoryConfiguration
        {
            private readonly List<HistoricValue> _values;
            private DateTime _lastAddedTime;

            /// <summary>
            /// Configures a history data set.
            /// </summary>
            /// <param name="measurement">The handle to the measurement. The value associated is not added to the list.</param>
            /// <param name="historyInterval">The resolution of the history data to keep</param>
            /// <param name="maxMeasurementAge">The maximum age of the history data</param>
            public MeasurementHistoryConfiguration(SensorMeasurement measurement, TimeSpan historyInterval, TimeSpan maxMeasurementAge)
            {
                Measurement = measurement;
                HistoryInterval = historyInterval;
                MaxMeasurementAge = maxMeasurementAge;
                _values = new List<HistoricValue>();
                _lastAddedTime = DateTime.MinValue;
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

            public bool TryAddMeasurement(IQuantity updatedValue, DateTime utcTimeOfMeasurement)
            {
                var now = utcTimeOfMeasurement;
                if (now > _lastAddedTime + HistoryInterval)
                {
                    _values.Add(new HistoricValue(now, updatedValue));
                    _lastAddedTime = now;
                    return true;
                }

                return false;
            }

            public bool TryAddMeasurement(IQuantity updatedValue)
            {
                return TryAddMeasurement(updatedValue, DateTime.UtcNow);
            }

            public IList<HistoricValue> Measurements()
            {
                // No clone here, could be expensive!
                return _values;
            }

            public void RemoveOldEntries()
            {
                if (MaxMeasurementAge == TimeSpan.MaxValue)
                {
                    return;
                }

                var oldest = DateTime.UtcNow - MaxMeasurementAge;
                // Never remove the last entry
                for (int i = 0; i < _values.Count - 1; i++)
                {
                    if (_values[i].MeasurementTime < oldest)
                    {
                        _values.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }
}
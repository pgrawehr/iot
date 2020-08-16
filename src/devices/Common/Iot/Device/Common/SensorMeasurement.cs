using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    public class SensorMeasurement : INotifyPropertyChanged
    {
        private SensorLocation _sensorLocation;
        private SensorMedium _sensorMedium;

        private IQuantity _value;

        public SensorMeasurement(IQuantity value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            SensorLocation = SensorLocation.Undefined;
            SensorMedium = SensorMedium.Undefined;
        }

        public SensorMeasurement(IQuantity value, SensorLocation location, SensorMedium medium)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            SensorLocation = location;
            SensorMedium = medium;
        }

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
        /// Updates the value of the sensor.
        /// Only values with the same physical quantity than the current instance already has are accepted.
        /// </summary>
        public IQuantity Value
        {
            get
            {
                return _value;
            }

            set
            {
                if (_value.Type != value.Type)
                {
                    throw new InvalidOperationException($"This {nameof(SensorMeasurement)} contains {_value.Type}, you cannot change it to {value.Type}.");
                }

                _value = value;
                OnPropertyChanged();
                ValueChanged?.Invoke(this);
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
    }
}

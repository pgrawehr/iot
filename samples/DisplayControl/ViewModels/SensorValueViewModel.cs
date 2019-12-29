using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;

namespace DisplayControl.ViewModels
{
    public class SensorValueViewModel : ViewModelBase
    {
        private SensorValueSource _sensorValueSource;
        private string _valueDescription;
        private string _valueAsString;
        private string _unit;

        public SensorValueViewModel()
        {
            _valueDescription = string.Empty;
            _sensorValueSource = null;
        }

        public SensorValueViewModel(SensorValueSource source)
            : this()
        {
            _sensorValueSource = source;
            UpdateValuesFromSource();
        }

        public string ValueDescription
        {
            get
            {
                return _valueDescription;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _valueDescription, value);
            }
        }

        public string ValueAsString
        {
            get
            {
                return _valueAsString;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _valueAsString, value);
            }
        }

        public string Unit
        {
            get
            {
                return _unit;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _unit, value);
            }
        }

        private void UpdateValuesFromSource()
        {
            if (_sensorValueSource == null)
            {
                ValueDescription = string.Empty;
                ValueAsString = string.Empty;
                Unit = string.Empty;
                return;
            }
            ValueDescription = _sensorValueSource.ValueDescription;
            ValueAsString = _sensorValueSource.ValueAsString;
            Unit = _sensorValueSource.Unit;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Avalonia.Media;
using Avalonia.Threading;
using Iot.Device.Common;
using ReactiveUI;

namespace DisplayControl.ViewModels
{
    public class SensorValueViewModel : ViewModelBase
    {
        private SensorMeasurement _sensorValueSource;
        private string _valueDescription;
        private string _valueAsString;
        private string _unit;
        private IBrush _statusColor;
        private long _lastUpdateTicks;

        public SensorValueViewModel()
        {
            _valueDescription = string.Empty;
            _sensorValueSource = null;
            _lastUpdateTicks = Environment.TickCount64;
        }

        public SensorValueViewModel(SensorMeasurement source)
            : this()
        {
            _sensorValueSource = source ?? throw new ArgumentNullException(nameof(source));
            UpdateValuesFromSource();
            source.PropertyChanged += SourcePropertyChanged;
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

        public IBrush StatusColor
        {
            get
            {
                return _statusColor;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _statusColor, value);
            }
        }

        public SensorMeasurement Source
        {
            get
            {
                return _sensorValueSource;
            }
        }

        private void UpdateValuesFromSource()
        {
            if (_sensorValueSource == null)
            {
                ValueDescription = string.Empty;
                ValueAsString = string.Empty;
                Unit = string.Empty;
                StatusColor = new SolidColorBrush(SystemDrawing.FromName("Gray"));
                return;
            }
            ValueDescription = _sensorValueSource.Name;
            ValueAsString = _sensorValueSource.ToString();
            // Get the unit abbreviation string (Unit.ToString() would give the full name)
            if (_sensorValueSource.Value != null)
            {
                Unit = _sensorValueSource.Value.ToString("a", CultureInfo.CurrentCulture);
            }
            else
            {
                Unit = string.Empty;
            }

            if (_sensorValueSource.Status.HasFlag(SensorMeasurementStatus.Warning))
            {
                StatusColor = new SolidColorBrush(SystemDrawing.FromName("Yellow"));
            }
            else if (_sensorValueSource.Status.HasFlag(SensorMeasurementStatus.NoData))
            { 
                StatusColor = new SolidColorBrush(SystemDrawing.FromName("Gray"));
            }
            else if (_sensorValueSource.Status.HasFlag(SensorMeasurementStatus.SensorError))
            {
                StatusColor = new SolidColorBrush(SystemDrawing.FromName("Red"));
            }
            else
            {
                StatusColor = new SolidColorBrush(SystemDrawing.FromName("White"));
            }
        }

        private void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            long ticksNow = Environment.TickCount64;
            if (_lastUpdateTicks + 1000 > ticksNow)
            {
                return;
            }

            _lastUpdateTicks = ticksNow;
            if (Dispatcher.UIThread.CheckAccess())
            {
                UpdateValuesFromSource();
                return;
            }
            Dispatcher.UIThread.Post(() =>
            {
                UpdateValuesFromSource();
            });
        }
    }
}

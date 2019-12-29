using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;

namespace DisplayControl.ViewModels
{
    public class SensorValueViewModel : ViewModelBase
    {
        private SensorValueSource _sensorValueSource;
        private string _valueDescription;
        private string _valueAsString;
        private string _unit;
        private IBrush _statusColor;

        public SensorValueViewModel()
        {
            _valueDescription = string.Empty;
            _sensorValueSource = null;
        }

        public SensorValueViewModel(SensorValueSource source)
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

        public SensorValueSource Source
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
            ValueDescription = _sensorValueSource.ValueDescription;
            ValueAsString = _sensorValueSource.ValueAsString;
            Unit = _sensorValueSource.Unit;
            if (_sensorValueSource.WarningLevel == WarningLevel.None)
            {
                StatusColor = new SolidColorBrush(SystemDrawing.FromName("White"));
            }
            else if (_sensorValueSource.WarningLevel == WarningLevel.NoData)
            { 
                StatusColor = new SolidColorBrush(SystemDrawing.FromName("Gray"));
            }
            else if (_sensorValueSource.WarningLevel == WarningLevel.Error)
            {
                StatusColor = new SolidColorBrush(SystemDrawing.FromName("Red"));
            }
            else if (_sensorValueSource.WarningLevel == WarningLevel.Warning)
            {
                StatusColor = new SolidColorBrush(SystemDrawing.FromName("Yellow"));
            }
        }

        private void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                UpdateValuesFromSource();
                return;
            }
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateValuesFromSource();
            });
        }
    }
}

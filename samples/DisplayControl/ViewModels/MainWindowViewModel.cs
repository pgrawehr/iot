using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Iot.Device.Common;
using ReactiveUI;

namespace DisplayControl.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string m_status;
        private IBrush m_statusColor;
        private bool m_cancel;
        private ObservableCollection<SensorValueViewModel> m_allViewModels;
        private ObservableCollection<SensorValueViewModel> m_sensorValueViewModels;
        private Size _size;
        private double _clientHeight;
        private double _headerHeight;
        private bool _displayLocked;
        private bool _deviationEnabled;
        private bool _useHeadingFromHandheld;
        private bool _forceTankSensorEnable;
        private Func<SensorValueViewModel, bool> m_filterFunc;

        public MainWindowViewModel()
        {
            m_allViewModels = new ObservableCollection<SensorValueViewModel>();
            m_sensorValueViewModels = new ObservableCollection<SensorValueViewModel>();
            m_filterFunc = (x) => true;
            Status = "System initialized";
            StatusColor = new SolidColorBrush(SystemDrawing.FromName("Green"));
            Cancel = false;
            DisplayLocked = false;
            _deviationEnabled = true;
            _useHeadingFromHandheld = false;
            _forceTankSensorEnable = false;
        }

        public MainWindowViewModel(DataContainer dataContainer)
            : this()
        {
            _size = new Size(400, 400);
            _clientHeight = 350;
            _headerHeight = 50;
            DataContainer = dataContainer;
            // Get value from settings
            DeviationEnabled = dataContainer.IsDeviationCorrectionEnabled();
            foreach (var elem in dataContainer.SensorValueSources)
            {
                m_allViewModels.Add(new SensorValueViewModel(elem));
            }

            UseHeadingFromHandheld = DataContainer.IsHandheldHeadingEnabled();
            UpdateVisibleModels();
        }

        public event Action DoClose;

        public string Status
        {
            get
            {
                return m_status;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref m_status, value);
            }
        }

        public IBrush StatusColor
        {
            get
            {
                return m_statusColor;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref m_statusColor, value);
            }
        }

        public bool Cancel
        {
            get
            {
                return m_cancel;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref m_cancel, value);
            }
        }

        public DataContainer DataContainer 
        { 
            get;
            private set;
        }

        public ObservableCollection<SensorValueViewModel> SensorValues
        {
            get
            {
                return m_sensorValueViewModels;
            }
        }

        public Size Size
        {
            get
            {
                return _size;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _size, value);
            }
        }

        public double HeaderHeight
        {
            get
            {
                return _headerHeight;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _headerHeight, value);
            }
        }

        public double ClientHeight
        {
            get
            {
                return _clientHeight;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _clientHeight, value);
            }
        }

        public bool DisplayLocked
        {
            get
            {
                return _displayLocked;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _displayLocked, value);
            }
        }

        public bool DeviationEnabled
        {
            get
            {
                return _deviationEnabled;
            }

            set
            {
                this.RaiseAndSetIfChanged(ref _deviationEnabled, value);
            }
        }

        public bool UseHeadingFromHandheld
        {
            get
            {
                return _useHeadingFromHandheld;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _useHeadingFromHandheld, value);
            }
        }

        public bool ForceTankSensorEnable
        {
            get
            {
                return _forceTankSensorEnable;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _forceTankSensorEnable, value);
            }
        }

        public void ActivateValueSingle(SensorValueViewModel vm)
        {
            DataContainer.ActiveValueSourceSingle = vm.Source;
        }

        public void ActivateValueUpper(SensorValueViewModel vm)
        {
            DataContainer.ActiveValueSourceUpper = vm.Source;
        }

        public void ActivateValueLower(SensorValueViewModel vm)
        {
            DataContainer.ActiveValueSourceLower = vm.Source;
        }

        public void SetStatus(string text, string color)
        {
            StatusColor = new SolidColorBrush(SystemDrawing.FromName(color));
            Status = text;
        }

        public void ExitCommand()
        {
            Cancel = true;
            DoClose?.Invoke();
        }

        public void ReinitDisplayCommand()
        {
            DataContainer.ReinitDisplay();
        }

        public void EnableDisableDeviationCorrection()
        {
            DeviationEnabled = !DeviationEnabled;
            DataContainer.EnableDeviationCorrection(DeviationEnabled);
        }

        public void EnableDisableHandheldForHeading()
        {
            UseHeadingFromHandheld = !UseHeadingFromHandheld;
            DataContainer.UseHandheldForHeading(UseHeadingFromHandheld);
        }

        public void EnableDisableForceTankSensorEnable()
        {
            ForceTankSensorEnable = !ForceTankSensorEnable;
            DataContainer.ForceTankSensorEnable(ForceTankSensorEnable);
        }

        public void LockDisplay()
        {
            DisplayLocked = !DisplayLocked;
            DataContainer.LockDisplay(DisplayLocked);
        }

        public void SendTestMessage()
        {
            DataContainer.SendAisTestMessage();
        }

        private void UpdateVisibleModels()
        {
            m_sensorValueViewModels.Clear();
            foreach (var model in m_allViewModels.Where(m_filterFunc))
            {
                m_sensorValueViewModels.Add(model);
            }
        }

        public void FilterForEngine()
        {
            m_filterFunc = x => x.Source.SensorSource == SensorSource.Engine;
            UpdateVisibleModels();
        }

        public void FilterForAis()
        {
            m_filterFunc = x => x.Source.SensorSource == SensorSource.Ais;
            UpdateVisibleModels();
        }

        public void FilterForWeather()
        {
            m_filterFunc = x => x.Source.SensorSource == SensorSource.Wind ||
                                x.Source.SensorSource == SensorSource.Air || 
                                x.Source == SensorMeasurement.WaterTemperature;
            UpdateVisibleModels();
        }

        public void FilterShowAll()
        {
            m_filterFunc = x => true;
            UpdateVisibleModels();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && DataContainer != null)
            {
                SetStatus("Shutting down...", "Yellow");
                DataContainer.ShutDown();
                DataContainer = null;
            }

            base.Dispose(disposing);
        }

        public void SetSize(Size size, int reduceBy)
        {
            Size = size;
            ClientHeight = _size.Height - reduceBy;
        }
    }
}

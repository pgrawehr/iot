using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;
using Avalonia;
using Avalonia.Media;
using ReactiveUI;

namespace DisplayControl.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string m_status;
        private IBrush m_statusColor;
        private bool m_cancel;
        private ObservableCollection<SensorValueViewModel> m_sensorValueViewModels;
        private Size _size;
        private double _clientHeight;
        private Point _headerHeight;
        private bool _displayLocked;
        private bool _deviationEnabled;
        private bool _useHeadingFromHandheld;

        public MainWindowViewModel()
        {
            Status = "System initialized";
            StatusColor = new SolidColorBrush(SystemDrawing.FromName("Green"));
            Cancel = false;
            DisplayLocked = false;
            _deviationEnabled = true;
            _useHeadingFromHandheld = false;
        }

        public MainWindowViewModel(DataContainer dataContainer)
            : this()
        {
            _size = new Size(400, 350);
            _clientHeight = 100;
            _headerHeight = new Point(0, 50);
            DataContainer = dataContainer;
            // Get value from settings
            DeviationEnabled = dataContainer.IsDeviationCorrectionEnabled();
            m_sensorValueViewModels = new ObservableCollection<SensorValueViewModel>();
            foreach (var elem in dataContainer.SensorValueSources)
            {
                m_sensorValueViewModels.Add(new SensorValueViewModel(elem));
            }
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
                ClientHeight = _size.Height - 50;
            }
        }

        public Point HeaderHeight
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

        public void EnableDeviation()
        {
            DataContainer.EnableDeviationCorrection(true);
        }

        public void LockDisplay()
        {
            DisplayLocked = !DisplayLocked;
            DataContainer.LockDisplay(DisplayLocked);
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
    }
}

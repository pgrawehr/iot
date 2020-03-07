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
        private SensorValueViewModel m_selectedViewModel;
        private Size _size;
        private double _clientHeight;
        private Point _headerHeight;

        public MainWindowViewModel()
        {
            Status = "System initialized";
            StatusColor = new SolidColorBrush(SystemDrawing.FromName("Green"));
            Cancel = false;
        }

        public MainWindowViewModel(DataContainer dataContainer)
            : this()
        {
            _size = new Size(400, 350);
            _clientHeight = 100;
            _headerHeight = new Point(0, 50);
            DataContainer = dataContainer;
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

        public SensorValueViewModel SelectedViewModel
        {
            get
            {
                return m_selectedViewModel;
            }
            private set
            {
                this.RaiseAndSetIfChanged(ref m_selectedViewModel, value);
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

        public void ActivateValueSingle(SensorValueViewModel vm)
        {
            SelectedViewModel = vm;
            DataContainer.ActiveValueSourceSingle = vm.Source;
        }

        public void ActivateValueUpper(SensorValueViewModel vm)
        {
            SelectedViewModel = vm;
            DataContainer.ActiveValueSourceUpper = vm.Source;
        }

        public void ActivateValueLower(SensorValueViewModel vm)
        {
            SelectedViewModel = vm;
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

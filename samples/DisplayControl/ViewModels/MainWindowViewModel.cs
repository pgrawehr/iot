using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;
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

        public MainWindowViewModel()
        {
            Status = "System initialized";
            StatusColor = new SolidColorBrush(SystemDrawing.FromName("Green"));
            Cancel = false;
        }

        public MainWindowViewModel(DataContainer dataContainer)
            : this()
        {
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

        public void ActivateValue(SensorValueViewModel vm)
        {
            SelectedViewModel = vm;
            DataContainer.ActiveValueSource = vm.Source;
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

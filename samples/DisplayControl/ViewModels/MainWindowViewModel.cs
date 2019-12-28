using System;
using System.Collections.Generic;
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

        public MainWindowViewModel(DataContainer dataContainer)
        {
            Status = "System initialized";
            StatusColor = new SolidColorBrush(SystemDrawing.FromName("Green"));
            Cancel = false;
            DataContainer = dataContainer;
            ListBoxElements = dataContainer.SensorValueSources;
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

        public IList<SensorValueSource> ListBoxElements
        {
            get;
            private set;
        }

        public SensorValueSource SelectedElement
        {
            get
            {
                return DataContainer.ActiveValueSource;
            }
            set
            {
                var v = DataContainer.ActiveValueSource;
                // Change before, so that the notified clients see the new value (but provide old value to the method)
                DataContainer.ActiveValueSource = value;
                this.RaiseAndSetIfChanged(ref v, value);
            }
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

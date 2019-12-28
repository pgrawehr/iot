using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using ReactiveUI;

namespace DisplayControl.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string m_status;
        private string m_statusColor;
        private bool m_cancel;

        public MainWindowViewModel()
        {
            Status = "System initialized";
            StatusColor = "Green";
            Cancel = false;
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

        public string StatusColor
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

        public void ExitCommand()
        {
            Status = "Shutting down...";
            Cancel = true;
            DoClose?.Invoke();
        }
    }
}

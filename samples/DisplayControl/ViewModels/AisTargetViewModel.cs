using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Avalonia.Media;
using Avalonia.Threading;
using Iot.Device.Common;
using Iot.Device.Nmea0183.Ais;
using ReactiveUI;

namespace DisplayControl.ViewModels
{
    public class AisTargetViewModel : ViewModelBase
    {
        private AisTarget _target;
        private IBrush _statusColor;
        private long _lastUpdateTicks;

        public AisTargetViewModel()
        {
            _target = new Ship(0);
            _lastUpdateTicks = Environment.TickCount64;
        }

        public AisTargetViewModel(AisTarget target)
        {
            _target = target;
            _lastUpdateTicks = Environment.TickCount64;
        }

        public string NameOrMmsi
        {
            get
            {
                return _target.NameOrMssi();
            }
        }

        public string FormattedMmsi
        {
            get
            {
                return _target.FormatMmsi();
            }
        }

        public uint Mmsi
        {
            get
            {
                return _target.Mmsi;
            }
        }

        public IBrush StatusColor
        {
            get
            {
                return _statusColor;
            }
        }

        private void UpdateValuesFromSource()
        {
            if (_target.Mmsi == 0)
            {
                _statusColor = new SolidColorBrush(SystemDrawing.FromName("Gray"));
                return;
            }

            var relPos = _target.RelativePosition;
            
            if (relPos == null)
            {
                _statusColor = new SolidColorBrush(SystemDrawing.FromName("Grey"));
            }
            else if (relPos.SafetyState == AisSafetyState.Dangerous)
            {
                _statusColor = new SolidColorBrush(SystemDrawing.FromName("Red"));
            }
            else if (relPos.SafetyState == AisSafetyState.Lost)
            {
                _statusColor = new SolidColorBrush(SystemDrawing.FromName("Yellow"));
            }
            else
            {
                _statusColor = new SolidColorBrush(SystemDrawing.FromName("Green"));
            }

            this.RaisePropertyChanged(nameof(StatusColor));
            this.RaisePropertyChanged(nameof(Mmsi));
            this.RaisePropertyChanged(nameof(NameOrMmsi));
        }

        public void UpdateFrom(AisTarget copyFrom)
        {
            _target = copyFrom;
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

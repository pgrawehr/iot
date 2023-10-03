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
using UnitsNet;
using UnitsNet.Units;

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
                if (_target is Ship s)
                {
                    return _target.FormatMmsi() + " (" + s.TransceiverClass + ")";
                }
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

        public string Data
        {
            get
            {
                return GetInformationAsString();
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
            this.RaisePropertyChanged(nameof(Data));
        }

        public string GetInformationAsString()
        {
            StringBuilder sb = new StringBuilder();
            var now = DateTimeOffset.Now;
            sb.Append($"Age: {_target.Age(now).TotalSeconds:F0}s ");
            var relPos = _target.RelativePosition;
            if (relPos != null && _target.Position.ContainsValidPosition())
            {
                sb.Append($"Dist: {FormatLength(relPos.Distance)} ");
                sb.Append($"CPA: {FormatLength(relPos.ClosestPointOfApproach)} ");
                sb.Append($"TCPA: {relPos.TimeToClosestPointOfApproach(now)} ");
                sb.Append($"Status: {relPos.SafetyState}");
            }
            else
            {
                sb.Append("No relative data or no valid target position");
            }

            return sb.ToString();
        }

        private string FormatLength(Length? length)
        {
            if (length.HasValue)
            {
                return FormatLength(length.Value);
            }

            return "N/A";
        }

        private string FormatLength(Length length)
        {
            if (length < Length.FromMeters(500))
            {
                length = length.ToUnit(LengthUnit.Meter);
                return length.Value.ToString("F1", CultureInfo.CurrentCulture) + "m";
            }
            length = length.ToUnit(LengthUnit.NauticalMile);
            double value = length.Value;
            // This shows the "magic number effect", meaning that at 999.9 nm, we get 5 digits instead of 4.
            if (value >= 1000)
            {
                return value.ToString("F0", CultureInfo.CurrentCulture) + "nm";
            }
            else if (value > 100)
            {
                return value.ToString("F1", CultureInfo.CurrentCulture) + "nm";
            }
            else
            {
                return value.ToString("F2", CultureInfo.CurrentCulture) + "nm";
            }
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

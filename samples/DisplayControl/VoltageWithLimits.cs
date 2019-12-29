using System;
using System.Collections.Generic;
using System.Text;

namespace DisplayControl
{
    public class VoltageWithLimits : ObservableValue<double>
    {
        private double _lowerLimit;
        private double _upperLimit;
        private bool _withinLimits;
        public VoltageWithLimits(string valueDescription, double lowerLimit, double upperLimit) : base(valueDescription, "V", -1.0)
        {
            _lowerLimit = lowerLimit;
            _upperLimit = upperLimit;
            _withinLimits = true;
        }

        public event Action<object, EventArgs> LimitTriggered;

        protected override void ValueChanged()
        {
            if (Value == -1.0)
            {
                WarningLevel = WarningLevel.NoData;
            }
            else if (Value < _lowerLimit)
            {
                WarningLevel = WarningLevel.Error;
            }
            else if (Value > _upperLimit)
            {
                WarningLevel = WarningLevel.Error;
            }
            else
            {
                WarningLevel = WarningLevel.None;
            }
            if (WarningLevel == WarningLevel.Error && _withinLimits)
            {
                _withinLimits = false;
                LimitTriggered?.Invoke(this, new EventArgs());
            }
            else if (!_withinLimits)
            {
                _withinLimits = true;
                LimitTriggered?.Invoke(this, new EventArgs());
            }
            base.ValueChanged();
        }
    }
}

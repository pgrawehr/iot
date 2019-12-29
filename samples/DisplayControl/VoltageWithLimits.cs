using System;
using System.Collections.Generic;
using System.Text;

namespace DisplayControl
{
    public class VoltageWithLimits : ObservableValue<double>
    {
        private double _lowerLimit;
        private double _upperLimit;
        public VoltageWithLimits(string valueDescription, double lowerLimit, double upperLimit) : base(valueDescription, "V", -1.0)
        {
            _lowerLimit = lowerLimit;
            _upperLimit = upperLimit;
        }

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
            base.ValueChanged();
        }
    }
}

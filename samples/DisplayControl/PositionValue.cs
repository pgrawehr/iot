using System;
using System.Collections.Generic;
using System.Text;
using Units;

namespace DisplayControl
{
    public class PositionValue : ObservableValue<GeographicPosition>
    {
        public PositionValue(string valueDescription) : base(valueDescription, string.Empty, new GeographicPosition())
        {
        }

        public override string ValueAsString
        {
            get
            {
                string north = GeographicPosition.GetLatitudeString(Value.Latitude);
                string east = GeographicPosition.GetLongitudeString(Value.Longitude);
                return north + "\n" + east;
            }
        }

        protected override void ValueChanged()
        {
            base.ValueChanged();
            if (Value.ContainsValidPosition() == false)
            {
                WarningLevel = WarningLevel.NoData;
            }
            else
            {
                WarningLevel = WarningLevel.None;
            }
        }
    }
}

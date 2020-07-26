using System;
using System.Globalization;

namespace Iot.Device.Nmea0183
{
    public partial class DeviationPoint
    {
        /// <summary>
        /// Generates a string representation of this object
        /// </summary>
        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "Heading {0}, Deviation {1}", MagneticHeading, Deviation);
        }
    }
}

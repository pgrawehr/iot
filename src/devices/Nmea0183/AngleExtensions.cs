using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnitsNet;

namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// Represents an angle value (i.e. for track or heading data)
    /// </summary>
    public static class AngleExtensions
    {
        /// <summary>
        /// Normalizes the angle so it is between 0° and 360° or between -180° and +180° respectively.
        /// </summary>
        /// <param name="self">Instance to normalize</param>
        /// <param name="toFullCircle">Set to true to normalize to 0-360°, otherwise normalizes to +/-180°</param>
        public static Angle Normalize(this Angle self, bool toFullCircle)
        {
            double r = self.Radians;
            if (toFullCircle)
            {
                if (r > Math.PI * 2)
                {
                    r = r % (Math.PI * 2);
                }

                if (r < 0)
                {
                    r = -(Math.Abs(r) % (Math.PI * 2));
                    if (r < 0)
                    {
                        r += Math.PI * 2;
                    }
                }
            }
            else
            {
                if (r > Math.PI)
                {
                    r = r % (Math.PI * 2);
                    if (r > Math.PI)
                    {
                        // Still above 180?
                        r -= Math.PI * 2;
                    }
                }

                if (r < -Math.PI)
                {
                    r = -(Math.Abs(r) % (Math.PI * 2));
                    if (r < -Math.PI)
                    {
                        r += Math.PI * 2;
                    }
                }
            }

            // Return in same unit as original input
            return Angle.FromRadians(r).ToUnit(self.Unit);
        }
    }
}

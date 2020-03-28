using System;
using System.Collections.Generic;
using System.Text;

namespace Units
{
    /// <summary>
    /// Extensions for positions
    /// </summary>
    public static class PositionExtensions
    {
        /// <summary>
        /// Normalizes the longitude to +/- 180°
        /// </summary>
        public static GeographicPosition NormalizeAngleTo180(this GeographicPosition position)
        {
            return new GeographicPosition(position.Latitude, NormalizeAngleTo180(position.Longitude), position.EllipsoidalHeight);
        }

        /// <summary>
        /// Normalizes the angle to +/- 180°
        /// </summary>
        public static double NormalizeAngleTo180(double angleDegree)
        {
            angleDegree %= 360;
            if (angleDegree <= -180)
            {
                angleDegree += 360;
            }
            else if (angleDegree > 180)
            {
                angleDegree -= 360;
            }

            return angleDegree;
        }

        /// <summary>
        /// Normalizes the longitude to [0..360°)
        /// </summary>
        public static GeographicPosition NormalizeAngleTo360(this GeographicPosition position)
        {
            return new GeographicPosition(position.Latitude, NormalizeAngleTo360(position.Longitude), position.EllipsoidalHeight);
        }

        /// <summary>
        /// Normalizes an angle to [0..360°)
        /// </summary>
        public static double NormalizeAngleTo360(double angleDegree)
        {
            angleDegree %= 360;
            if (angleDegree < 0)
            {
                angleDegree += 360;
            }

            return angleDegree;
        }
    }
}

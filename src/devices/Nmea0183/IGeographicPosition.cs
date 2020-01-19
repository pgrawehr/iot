using System;
using System.Collections.Generic;
using System.Text;

namespace Nmea0183
{
    /// <summary>
    /// Represents a position in the WGS84 Global coordinate system
    /// </summary>
    public interface IGeographicPosition
    {
        /// <summary>
        /// Latitude of point, in decimal degrees (Range -90° to +90°)
        /// </summary>
        double Latitude
        {
            get;
        }

        /// <summary>
        /// Longitude of point, in decimal degrees (Range depends on application, typical -180° to + 180° or 0° to 360°. May use normalization to convert one into the other)
        /// </summary>
        double Longitude
        {
            get;
        }

        /// <summary>
        /// Height over or below ellipsoid, in meters (Range typically from -200 to about 10000)
        /// </summary>
        double EllipsoidalHeight
        {
            get;
        }

        /// <summary>
        /// True if this instance contains a valid position (invalid is 0/0/0)
        /// </summary>
        bool ContainsValidPosition();

        /// <summary>
        /// True if the two points are about equal
        /// </summary>
        /// <param name="geographicPosition">The position to compare to</param>
        /// <returns>True if they're about equal (to a few cm)</returns>
        bool EqualPosition(IGeographicPosition geographicPosition);
    }
}

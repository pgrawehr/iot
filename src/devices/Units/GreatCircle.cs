using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591
namespace Units
{
    public static class GreatCircle
    {
        /// <summary>
        /// Semi-Mayor Axis of the WGS ellipsoid
        /// </summary>
        public const double WGS84_A = 6378137.0;

        /// <summary>
        /// Flattening "f" of the WGS84 ellipsoid (1.0 / 298.25722357)
        /// </summary>
        public const double WGS84_F = 0.00335281066474;

        /// <summary>
        /// m per degree latitude
        /// </summary>
        public const double METERS_PER_DEGREEE_LATITUDE = 110563.789;

        /// <summary>
        /// m per degree longitude on the equator
        /// </summary>
        public const double METERS_PER_DEGREE_LONGITUDE = 111312.267;

        private static GeoidCalculations.geod_geodesic _geod;

        static GreatCircle()
        {
            GeoidCalculations.geod_init(out _geod, WGS84_A, WGS84_F);
        }

        public static void DistAndDir(GeographicPosition position1, GeographicPosition position2, out double distance, out double direction)
        {
            DistAndDir(position1.Latitude, position1.Longitude, position2.Latitude, position2.Longitude, out distance, out direction);
        }

        public static void DistAndDir(double latitude1,  double longitude1, double latitude2, double longitude2, out double distance, out double direction)
        {
            GeoidCalculations.geod_inverse(_geod, latitude1, longitude1, latitude2, longitude2, out distance, out direction, out _);
        }

        public static void CalcCoords(double startLatitude, double startLongitude, double direction, double distance, out double resultLatitude, out double resultLongitude)
        {
            GeoidCalculations.geod_direct(_geod, startLatitude, startLongitude, direction, distance, out resultLatitude, out resultLongitude, out _);
        }

        /// <summary>
        /// Convert a value to radians.
        /// </summary>
        public static double DegreesToRadians(double val)
        {
            double ret = ((2 * Math.PI * val) / 360);
            return ret;
        }

        /// <summary>
        /// Converts an angle in aviatic definition to mathematic definition.
        /// Aviatic angles are in degrees, where 0 degrees is north, counting clockwise, mathematic angles
        /// are in radians, starting east and going counterclockwise.
        /// </summary>
        /// <param name="val">Aviatic angle, degrees</param>
        /// <returns>Mathematic angle, radians, fast-normalized to 0..2Pi</returns>
        public static double AviaticToRadians(double val)
        {
            double ret = ((-val) + 90.0);
            ret = DegreesToRadians(ret);
            if (ret >= 2 * Math.PI)
            {
                ret -= 2 * Math.PI;
            }

            if (ret < 0)
            {
                ret += 2 * Math.PI;
            }

            return ret;
        }

        /// <summary>
        /// Calculate the difference between two angles, return degrees.
        /// </summary>
        /// <param name="a">First angle, in degrees</param>
        /// <param name="b">Second angle, in degrees</param>
        /// <returns>Difference, ranging -180 to +180, in degrees.</returns>
        public static double AngleDifferenceSignedDegrees(double a, double b)
        {
            double val = a - b;
            if (val > 180)
            {
                val -= 360;
            }

            if (val < -180)
            {
                val += 360;
            }

            return val;
        }

        /// <summary>
        /// Converts an angle in radians to angle in decimal degrees
        /// </summary>
        /// <param name="radians">Angle in radians (0-2*Pi)</param>
        /// <returns>Angle in decimal degrees (0-360)</returns>
        public static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        /// <summary>
        /// Convert angle from mathematic to aviatic.
        /// See also AviaticToRadians()
        /// </summary>
        /// <param name="val">Mathematic value in radians</param>
        /// <returns>Aviatic value in degrees</returns>
        public static double RadiansToAviatic(double val)
        {
            double ret = RadiansToDegrees(val);
            ret = ((-ret) + 90.0);
            if (ret < 0)
            {
                ret += 360;
            }

            return ret;
        }
    }
}

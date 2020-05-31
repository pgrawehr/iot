using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
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

        public static void DistAndDir(GeographicPosition position1, GeographicPosition position2, out double distance, out double directionAtStart, out double directionAtEnd)
        {
            DistAndDir(position1.Latitude, position1.Longitude, position2.Latitude, position2.Longitude, out distance, out directionAtStart, out directionAtEnd);
        }

        public static void DistAndDir(double latitude1,  double longitude1, double latitude2, double longitude2, out double distance, out double direction)
        {
            GeoidCalculations.geod_inverse(_geod, latitude1, longitude1, latitude2, longitude2, out distance, out direction, out _);
        }

        public static void DistAndDir(double latitude1, double longitude1, double latitude2, double longitude2, out double distance, out double directionAtStart, out double directionAtEnd)
        {
            GeoidCalculations.geod_inverse(_geod, latitude1, longitude1, latitude2, longitude2, out distance, out directionAtStart, out directionAtEnd);
        }

        /// <summary>
        /// Computes cross-track error, that is the distance the current position is away from the route from origin to destination
        /// </summary>
        /// <param name="origin">Start of current leg</param>
        /// <param name="destination">End of current leg</param>
        /// <param name="currentPosition">Current position</param>
        /// <param name="crossTrackError">The distance perpendicular to the leg. Positive if the current position is to the right of the leg.</param>
        /// <param name="distanceTogoAlongRoute">Distance to go on track (with current position projected back to the leg)</param>
        /// <remarks>Accuracy may be limited for distances &gt; 100km</remarks>
        public static void CrossTrackError(GeographicPosition origin, GeographicPosition destination, GeographicPosition currentPosition,
            out Length crossTrackError, out Length distanceTogoAlongRoute)
        {
            DistAndDir(origin, destination, out double distanceOriginToDestination, out double directionOriginToDestination, out double trackEndDirection);
            DistAndDir(currentPosition, destination, out double distanceToDestination, out double currentToDestination);

            Angle angleDiff = AngleExtensions.Difference(Angle.FromDegrees(trackEndDirection), Angle.FromDegrees(currentToDestination));
            distanceTogoAlongRoute = Length.FromMeters(Math.Cos(angleDiff.Radians) * distanceToDestination);
            crossTrackError = Length.FromMeters(Math.Sin(angleDiff.Radians) * distanceToDestination);
        }

        /// <summary>
        /// Calculate the velocity towards (or away from) the target. This is often also called VMG (=Velocity made good)
        /// </summary>
        /// <param name="destination">Target waypoint</param>
        /// <param name="currentPosition">Current position</param>
        /// <param name="currentSpeed">Current speed over ground</param>
        /// <param name="currentTrack">Current track (course over ground)</param>
        /// <returns>Speed towards target. Negative if moving away from target</returns>
        public static Speed CalculateVelocityTowardsTarget(GeographicPosition destination, GeographicPosition currentPosition, Speed currentSpeed, Angle currentTrack)
        {
            DistAndDir(currentPosition, destination, out double distanceToDestination, out double currentToDestination);
            return Speed.Zero;
        }

        public static GeographicPosition CalcCoords(GeographicPosition start, double direction, double distance)
        {
            GeoidCalculations.geod_direct(_geod, start.Latitude, start.Longitude, direction, distance, out double resultLatitude, out double resultLongitude, out _);
            return new GeographicPosition(resultLatitude, resultLongitude, start.EllipsoidalHeight);
        }

        public static void CalcCoords(double startLatitude, double startLongitude, double direction, double distance, out double resultLatitude, out double resultLongitude)
        {
            GeoidCalculations.geod_direct(_geod, startLatitude, startLongitude, direction, distance, out resultLatitude, out resultLongitude, out _);
        }

        public static IList<GeographicPosition> CalculateRoute(GeographicPosition start, GeographicPosition end, double distanceStep)
        {
            IList<GeographicPosition> ret = new List<GeographicPosition>();
            GeoidCalculations.geod_geodesicline line;
            GeoidCalculations.geod_inverseline(out line, _geod, start.Latitude, start.Longitude, end.Latitude, end.Longitude, 0);
            double distanceTotal = line.s13;
            for (double d = 0; d <= distanceTotal; d += distanceStep)
            {
                GeoidCalculations.geod_position(line, d, out double lat2, out double lon2, out _);
                ret.Add(new GeographicPosition(lat2, lon2, 0));
            }

            return ret;
        }

        public static IList<GeographicPosition> CalculateRoute(GeographicPosition start, double direction, double distance, double distanceStep)
        {
            IList<GeographicPosition> ret = new List<GeographicPosition>();
            GeoidCalculations.geod_geodesicline line;
            GeoidCalculations.geod_directline(out line, _geod, start.Latitude, start.Longitude, direction, distance, 0);
            double distanceTotal = distance;
            for (double d = 0; d <= distanceTotal; d += distanceStep)
            {
                GeoidCalculations.geod_position(line, d, out double lat2, out double lon2, out _);
                ret.Add(new GeographicPosition(lat2, lon2, 0));
            }

            return ret;
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

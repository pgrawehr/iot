using System;
using System.Collections.Generic;
using System.Text;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// A point along a route
    /// </summary>
    public sealed class RoutePoint : IEquatable<RoutePoint>
    {
        public RoutePoint(string routeName, int indexInRoute, int totalPointsInRoute, string waypointName, GeographicPosition position,
            Angle? bearingToNextWaypoint, Length? distanceToNextWaypoint)
        {
            RouteName = routeName;
            IndexInRoute = indexInRoute;
            TotalPointsInRoute = totalPointsInRoute;
            WaypointName = waypointName;
            Position = position;
            BearingToNextWaypoint = bearingToNextWaypoint;
            DistanceToNextWaypoint = distanceToNextWaypoint;
        }

        public string RouteName
        {
            get;
            internal set;
        }

        public int IndexInRoute
        {
            get;
            internal set;
        }

        public int TotalPointsInRoute
        {
            get;
            internal set;
        }

        public string WaypointName
        {
            get;
        }

        public GeographicPosition Position
        {
            get;
        }

        /// <summary>
        /// True bearing from this waypoint to the next
        /// </summary>
        public Angle? BearingToNextWaypoint
        {
            get;
            set;
        }

        public Length? DistanceToNextWaypoint
        {
            get;
            set;
        }

        /// <summary>
        /// Two points are considered equal if the name and the position are equal. The other properties are NMEA-internals and are
        /// not directly related to the function of the waypoint for the user
        /// </summary>
        public bool Equals(RoutePoint? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return WaypointName == other.WaypointName && Equals(Position, other.Position);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || (obj is RoutePoint other && Equals(other));
        }

        public override int GetHashCode()
        {
            return WaypointName.GetHashCode() ^ Position.GetHashCode();
        }
    }
}

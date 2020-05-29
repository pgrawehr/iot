using System;
using System.Collections.Generic;
using System.Text;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    public sealed class RoutePoint
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
        }

        public int IndexInRoute
        {
            get;
        }

        public int TotalPointsInRoute
        {
            get;
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
    }
}

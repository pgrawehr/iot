﻿using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// State of autopilot.
    /// Not all errors are serious problems, some just mean it is inactive, because it has nothing to do.
    /// </summary>
    public enum AutopilotErrorState
    {
        /// <summary>
        /// State is unknown
        /// </summary>
        Unknown,
        NoRoute,

        /// <summary>
        /// The current route has no or incomplete waypoints. Normally this resolves itself after a few seconds, when the nav software
        /// continues transmitting data.
        /// </summary>
        WaypointsWithoutPosition,

        /// <summary>
        /// Operating as slave, RMB input sentence is present, route is known
        /// </summary>
        OperatingAsSlave,

        /// <summary>
        /// The input route contains duplicate waypoints. This causes confusion and is therefore considered an error.
        /// </summary>
        RouteWithDuplicateWaypoints,

        /// <summary>
        /// A route is present
        /// </summary>
        RoutePresent,
        DirectGoto,
        InvalidNextWaypoint,

        /// <summary>
        /// Full self-controlled operation
        /// </summary>
        OperatingAsMaster
    }
}
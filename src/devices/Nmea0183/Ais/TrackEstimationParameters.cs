// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace Iot.Device.Nmea0183.Ais
{
    /// <summary>
    /// Configurable parameters that define the behavior of the AIS target movement estimation.
    /// </summary>
    public record TrackEstimationParameters
    {
        public TimeSpan StartTimeOffset { get; set; } = TimeSpan.FromMinutes(20);
        public TimeSpan NormalStepSize { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan EndTimeOffset { get; set; } = TimeSpan.FromMinutes(60);

        public bool WarnIfGnssMissing { get; set; } = true;

        public TimeSpan AisSafetyCheckInterval { get; set; } = TimeSpan.FromSeconds(5);
        public Length WarningDistance { get; set; } = Length.FromNauticalMiles(1);
        public TimeSpan WarningTime { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Maximum age of the position record for a given ship to consider it valid.
        /// If this is set to a high value, there's a risk of calculating TCPA/CPA based on outdated data.
        /// </summary>
        public TimeSpan TargetLostTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum age of our own position to consider it valid
        /// </summary>
        public TimeSpan MaximumPositionAge { get; set; } = TimeSpan.FromSeconds(20);
    }
}

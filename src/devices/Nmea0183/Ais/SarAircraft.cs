﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using UnitsNet;

namespace Iot.Device.Nmea0183.Ais
{
    public class SarAircraft : AisTarget
    {
        /// <summary>
        /// Unfortunately, the sar message does not include a "name" field.
        /// </summary>
        private const string SarAircraftName = "SAR Aircraft";

        public SarAircraft(uint mmsi)
            : base(mmsi)
        {
            Name = SarAircraftName;
        }

        public Speed? Speed { get; set; }

        public Angle? CourseOverGround { get; set; }
    }
}
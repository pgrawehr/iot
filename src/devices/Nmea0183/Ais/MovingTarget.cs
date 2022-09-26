// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using UnitsNet;

namespace Iot.Device.Nmea0183.Ais
{
    public abstract record MovingTarget : AisTarget
    {
        protected MovingTarget(uint mmsi)
            : base(mmsi)
        {
        }

        public RotationalSpeed? RateOfTurn { get; set; }
        public Angle CourseOverGround { get; set; }
        public Speed SpeedOverGround { get; set; }
        public Angle? TrueHeading { get; set; }
    }
}

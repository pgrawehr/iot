// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Iot.Device.Common;
using UnitsNet;
using UnitsNet.Units;

namespace Iot.Device.Nmea0183.Ais
{
    public static class AisTargetExtensions
    {
        public static Length DistanceTo(this AisTarget self, AisTarget other)
        {
            GreatCircle.DistAndDir(self.Position, other.Position, out Length distance, out _);
            return distance.ToUnit(LengthUnit.NauticalMile);
        }
    }
}

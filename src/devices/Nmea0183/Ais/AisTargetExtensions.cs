// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

        /// <summary>
        /// Returns the age of this target, relative to the indicated time.
        /// </summary>
        /// <param name="self">The target under investigation</param>
        /// <param name="toTime">The time to compare to (often <see cref="DateTimeOffset.UtcNow"/>)</param>
        /// <returns></returns>
        public static TimeSpan Age(this AisTarget self, DateTimeOffset toTime)
        {
            return toTime - self.LastSeen;
        }
    }
}

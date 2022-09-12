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

        public static ShipRelativePosition RelativePositionTo(this AisTarget self, AisTarget other)
        {
            GreatCircle.DistAndDir(self.Position, other.Position, out Length distance, out Angle direction);

            Angle? relativeDirection = null;

            if (self is Ship s && s.TrueHeading.HasValue)
            {
                relativeDirection = (direction - s.TrueHeading.Value).Normalize(false);
            }

            return new ShipRelativePosition(self, other, distance, direction)
            {
                RelativeDirection = relativeDirection,
            };
        }

        /// <summary>
        /// Estimates where a ship will be after some time.
        /// </summary>
        /// <param name="ship">The ship to extrapolate</param>
        /// <param name="extrapolationTime">The time to move forward. Very large values are probably useless, because the ship might start a turn</param>
        /// <param name="stepSize">The extrapolation step size. Smaller values will lead to better estimation, but are compuationally expensive</param>
        /// <returns>A <see cref="Ship"/> instance with the new position and course</returns>
        /// <exception cref="ArgumentOutOfRangeException">Stepsize or extrapolation time are not positive</exception>
        public static Ship EstimatePosition(this Ship ship, TimeSpan extrapolationTime, TimeSpan stepSize)
        {
            if (stepSize <= TimeSpan.FromMilliseconds(1))
            {
                throw new ArgumentOutOfRangeException(nameof(stepSize),
                    "Step size must be positive and greater than 1ms");
            }

            if (extrapolationTime <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(extrapolationTime), "extrapolationTime must be positive");
            }

            DateTimeOffset currentTime = ship.LastSeen;
            Ship newShip = ship with { Position = new GeographicPosition(ship.Position) };
            Angle cogChange = Angle.Zero;
            if (ship.RateOfTurn.HasValue)
            {
                var rot = ship.RateOfTurn.Value;
                cogChange = rot * stepSize;
            }

            while (currentTime < ship.LastSeen + extrapolationTime)
            {
                currentTime += stepSize;
                newShip.CourseOverGround = (newShip.CourseOverGround + cogChange).Normalize(true);
                Length distanceDuringStep = stepSize * newShip.SpeedOverGround;
                newShip.Position =
                    GreatCircle.CalcCoords(newShip.Position, newShip.CourseOverGround, distanceDuringStep);
            }

            return newShip;
        }
    }
}

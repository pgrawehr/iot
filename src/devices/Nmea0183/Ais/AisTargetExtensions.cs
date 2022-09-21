// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
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

        public static ShipRelativePosition RelativePositionTo(this Ship self, AisTarget other, DateTimeOffset now, TrackEstimationParameters parameters)
        {
            var self1 = EstimatePosition(self, now, parameters.NormalStepSize);

            // Todo: If other is a SAR aircraft, convert it to a ship, because it moves
            Ship? otherAsShip = other as Ship;
            Length distance;
            Angle direction;

            GreatCircle.DistAndDir(self.Position, other.Position, out distance, out direction);

            Angle? relativeDirection = null;

            if (self1.TrueHeading.HasValue)
            {
                relativeDirection = (direction - self1.TrueHeading.Value).Normalize(false);
            }

            AisSafetyState state = AisSafetyState.Safe;

            if (other.LastSeen + parameters.TargetLostTimeout < now)
            {
                // For a lost target, don't do a full computation
                state = AisSafetyState.Lost;
                otherAsShip = null;
            }

            if (otherAsShip == null)
            {
                // The other is not a ship - Assume static position
                if (distance < parameters.WarningDistance)
                {
                    state = AisSafetyState.Dangerous;
                }

                return new ShipRelativePosition(self, other, distance, direction, state)
                {
                    RelativeDirection = relativeDirection,
                };
            }
            else
            {
                var otherPos = other.Position;
                GreatCircle.DistAndDir(self1.Position, otherPos, out distance, out direction);
                List<Ship> thisTrack = GetEstimatedTrack(self1, now - parameters.StartTimeOffset, now + parameters.EndTimeOffset, parameters.NormalStepSize);
                List<Ship> otherTrack = GetEstimatedTrack(otherAsShip, now - parameters.StartTimeOffset, now + parameters.EndTimeOffset, parameters.NormalStepSize);

                if (thisTrack.Count != otherTrack.Count || thisTrack.Count < 1)
                {
                    // The two lists must have equal length and contain at least one element
                    throw new InvalidDataException("Internal error: Data structures inconsistent");
                }

                Length minimumDistance = Length.MaxValue;
                DateTimeOffset timeOfMinimumDistance = default;
                int usedIndex = 0;
                for (int i = 0; i < thisTrack.Count; i++)
                {
                    GreatCircle.DistAndDir(thisTrack[i].Position, otherTrack[i].Position, out Length distance1, out _, out _);
                    if (distance1 < minimumDistance)
                    {
                        minimumDistance = distance1;
                        timeOfMinimumDistance = thisTrack[i].LastSeen;
                        usedIndex = i;
                    }
                }

                // if the closest point is the first or the last element, we assume it's more than that, and leave the fields empty
                if (usedIndex == 0 || usedIndex == thisTrack.Count - 1)
                {
                    return new ShipRelativePosition(self, other, distance, direction, AisSafetyState.Unknown)
                    {
                        RelativeDirection = relativeDirection,
                        ClosestPointOfApproach = null,
                        TimeOfClosestPointOfApproach = null,
                    };
                }
                else
                {
                    var ret = new ShipRelativePosition(self, other, distance, direction, state)
                    {
                        RelativeDirection = relativeDirection,
                        // Todo: Should subtract the size of both ships here (idealy considering the direction of the ships hulls)
                        ClosestPointOfApproach = minimumDistance,
                        TimeOfClosestPointOfApproach = timeOfMinimumDistance,
                    };

                    var timeToClosest = ret.TimeToClosestPointOfApproach(now);
                    if (ret.ClosestPointOfApproach < parameters.WarningDistance &&
                        timeToClosest > -TimeSpan.FromMinutes(1) && timeToClosest < parameters.WarningTime)
                    {
                        ret.SafetyState = AisSafetyState.Dangerous;
                    }

                    return ret;
                }
            }
        }

        /// <summary>
        /// Estimates where a ship will be after some time.
        /// </summary>
        /// <param name="ship">The ship to extrapolate</param>
        /// <param name="extrapolationTime">The time to move. Very large values are probably useless, because the ship might start a turn.</param>
        /// <param name="stepSize">The extrapolation step size. Smaller values will lead to better estimation, but are compuationally expensive</param>
        /// <returns>A <see cref="Ship"/> instance with the estunated position and course</returns>
        /// <exception cref="ArgumentOutOfRangeException">Stepsize is not positive</exception>
        /// <remarks>The reference time is the position/time the last report was received from this ship. To be able to compare two ships, the
        /// times still need to be aligned.</remarks>
        public static Ship EstimatePosition(this Ship ship, TimeSpan extrapolationTime, TimeSpan stepSize)
        {
            if (stepSize <= TimeSpan.FromMilliseconds(1))
            {
                throw new ArgumentOutOfRangeException(nameof(stepSize),
                    "Step size must be positive and greater than 1ms");
            }

            if (extrapolationTime.Duration() < stepSize)
            {
                stepSize = extrapolationTime.Duration(); // one step only
            }

            DateTimeOffset currentTime = ship.LastSeen;
            Ship newShip = ship with { Position = new GeographicPosition(ship.Position), IsEstimate = true };
            Angle cogChange = Angle.Zero;
            if (ship.RateOfTurn.HasValue)
            {
                var rot = ship.RateOfTurn.Value;
                cogChange = rot * stepSize;
            }

            // Differentiate between moving forward and backward in time. Note that stepSize is expected
            // to be positive in either case
            if (extrapolationTime > TimeSpan.Zero)
            {
                while (currentTime < ship.LastSeen + extrapolationTime)
                {
                    currentTime += stepSize;
                    newShip.CourseOverGround = (newShip.CourseOverGround + cogChange).Normalize(true);
                    Length distanceDuringStep = stepSize * newShip.SpeedOverGround;
                    newShip.Position =
                        GreatCircle.CalcCoords(newShip.Position, newShip.CourseOverGround, distanceDuringStep);
                    newShip.LastSeen = currentTime;
                }
            }
            else
            {
                while (currentTime > ship.LastSeen + extrapolationTime) // extrapolationTime is negative here
                {
                    currentTime -= stepSize;
                    Length distanceDuringStep = -(stepSize * newShip.SpeedOverGround);
                    newShip.Position =
                        GreatCircle.CalcCoords(newShip.Position, newShip.CourseOverGround, distanceDuringStep);

                    // To get the closes possible inverse of the above, we correct the cog afterwards here
                    newShip.CourseOverGround = (newShip.CourseOverGround - cogChange).Normalize(true);
                    newShip.LastSeen = currentTime;
                }
            }

            return newShip;
        }

        public static Ship EstimatePosition(this Ship ship, DateTimeOffset time, TimeSpan stepSize)
        {
            TimeSpan delta = ship.Age(time);
            return EstimatePosition(ship, delta, stepSize);
        }

        public static List<Ship> GetEstimatedTrack(this Ship ship, DateTimeOffset startTime, DateTimeOffset endTime, TimeSpan stepSize)
        {
            if (stepSize <= TimeSpan.FromMilliseconds(1))
            {
                throw new ArgumentOutOfRangeException(nameof(stepSize),
                    "Step size must be positive and greater than 1ms");
            }

            if (startTime >= endTime)
            {
                throw new ArgumentException("startTime must be before endTime");
            }

            List<Ship> track = new List<Ship>();

            DateTimeOffset currentTime = startTime;
            Ship ship1 = EstimatePosition(ship, currentTime, stepSize);
            track.Add(ship1);
            while (currentTime < endTime)
            {
                currentTime += stepSize;
                ship1 = ship1.EstimatePosition(stepSize, stepSize); // One step ahead
                track.Add(ship1);
            }

            return track;
        }
    }
}

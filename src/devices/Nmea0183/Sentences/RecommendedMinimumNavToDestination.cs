using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Iot.Device.Nmea0183;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183.Sentences
{
    public class RecommendedMinimumNavToDestination : NmeaSentence
    {
        public static SentenceId Id => new SentenceId('R', 'M', 'B');
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        public RecommendedMinimumNavToDestination(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        public RecommendedMinimumNavToDestination(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            string overallStatus = ReadString(field);
            double? crossTrackError = ReadValue(field);
            string directionToSteer = ReadString(field);
            string previousWayPoint = ReadString(field);
            string nextWayPoint = ReadString(field);
            double? nextWayPointLatitude = ReadValue(field);
            CardinalDirection? nextWayPointHemisphere = (CardinalDirection?)ReadChar(field);
            double? nextWayPointLongitude = ReadValue(field);
            CardinalDirection? nextWayPointDirection = (CardinalDirection?)ReadChar(field);
            double? rangeToWayPoint = ReadValue(field);
            double? bearing = ReadValue(field);
            double? approachSpeed = ReadValue(field);
            string arrivalStatus = ReadString(field);

            if (overallStatus == "A")
            {
                Valid = true;
                if (directionToSteer == "R")
                {
                    CrossTrackError = -Length.FromNauticalMiles(crossTrackError.GetValueOrDefault(0));
                }
                else
                {
                    CrossTrackError = Length.FromNauticalMiles(crossTrackError.GetValueOrDefault(0));
                }

                PreviousWayPointName = previousWayPoint ?? string.Empty;
                NextWayPointName = nextWayPoint ?? string.Empty;
                double? latitude = RecommendedMinimumNavigationInformation.Nmea0183ToDegrees(nextWayPointLatitude, nextWayPointHemisphere);
                double? longitude = RecommendedMinimumNavigationInformation.Nmea0183ToDegrees(nextWayPointLongitude, nextWayPointDirection);

                if (latitude.HasValue && longitude.HasValue)
                {
                    NextWayPoint = new GeographicPosition(latitude.Value, longitude.Value, 0);
                }

                if (rangeToWayPoint.HasValue)
                {
                    DistanceToWayPoint = Length.FromNauticalMiles(rangeToWayPoint.Value);
                }

                if (bearing.HasValue)
                {
                    BearingToWayPoint = Angle.FromDegrees(bearing.Value);
                }

                if (approachSpeed.HasValue)
                {
                    ApproachSpeed = Speed.FromKnots(approachSpeed.Value);
                }

                if (arrivalStatus == "A")
                {
                    Arrived = true;
                }
                else
                {
                    Arrived = false;
                }
            }
        }

        public RecommendedMinimumNavToDestination(
            DateTimeOffset? dateTime,
            Length crossTrackError,
            string previousWayPointName,
            string nextWayPointName,
            GeographicPosition nextWayPoint,
            Length distanceToWayPoint,
            Angle bearingToWayPoint,
            Speed approachSpeedToWayPoint)
        : base(OwnTalkerId, Id, dateTime.GetValueOrDefault(DateTimeOffset.UtcNow))
        {
            CrossTrackError = crossTrackError;
            PreviousWayPointName = previousWayPointName;
            NextWayPointName = nextWayPointName;
            NextWayPoint = nextWayPoint;
            DistanceToWayPoint = distanceToWayPoint;
            BearingToWayPoint = bearingToWayPoint;
            ApproachSpeed = approachSpeedToWayPoint;
            Valid = true;
        }

        /// <summary>
        /// Cross track error. Positive: we are to the right of the desired route
        /// </summary>
        public Length CrossTrackError
        {
            get;
        }

        /// <summary>
        /// Name of previous waypoint
        /// </summary>
        public string PreviousWayPointName
        {
            get;
        }

        /// <summary>
        /// Name of next waypoint
        /// </summary>
        public string NextWayPointName
        {
            get;
        }

        /// <summary>
        /// Position of next waypoint (the waypoint we're heading to)
        /// </summary>
        public GeographicPosition NextWayPoint
        {
            get;
        }

        /// <summary>
        /// Distance to next waypoint
        /// </summary>
        public Length? DistanceToWayPoint
        {
            get;
        }

        /// <summary>
        /// True bearing to waypoint
        /// </summary>
        public Angle? BearingToWayPoint
        {
            get;
        }

        /// <summary>
        /// Speed of approach to the waypoint
        /// </summary>
        public Speed? ApproachSpeed
        {
            get;
        }

        /// <summary>
        /// True: Within arrival circle of waypoint
        /// </summary>
        public bool Arrived
        {
            get;
        }

        public override string ToNmeaMessage()
        {
            if (Valid)
            {
                StringBuilder b = new StringBuilder(256);
                b.Append("A,"); // Status = Valid
                if (CrossTrackError >= Length.Zero)
                {
                    b.AppendFormat(CultureInfo.InvariantCulture, "{0:F3},L,", CrossTrackError.NauticalMiles);
                }
                else
                {
                    b.AppendFormat(CultureInfo.InvariantCulture, "{0:F3},R,", -CrossTrackError.NauticalMiles);
                }

                b.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},", PreviousWayPointName, NextWayPointName);

                double? degrees = null;
                CardinalDirection? direction = null;
                if (NextWayPoint != null)
                {
                    (degrees, direction) =
                        RecommendedMinimumNavigationInformation.DegreesToNmea0183(NextWayPoint.Latitude, true);
                }

                if (degrees.HasValue && direction.HasValue)
                {
                    b.AppendFormat(CultureInfo.InvariantCulture, "{0:0000.00000},{1},", degrees.Value, (char)direction);
                }
                else
                {
                    b.Append(",,");
                }

                degrees = null;
                direction = null;
                if (NextWayPoint != null)
                {
                    (degrees, direction) =
                        RecommendedMinimumNavigationInformation.DegreesToNmea0183(NextWayPoint.Longitude, false);
                }

                if (degrees.HasValue && direction.HasValue)
                {
                    b.AppendFormat(CultureInfo.InvariantCulture, "{0:00000.00000},{1},", degrees.Value, (char)direction);
                }
                else
                {
                    b.Append(",,");
                }

                if (DistanceToWayPoint.HasValue)
                {
                    b.AppendFormat(CultureInfo.InvariantCulture, "{0:F3},", DistanceToWayPoint.Value.NauticalMiles);
                }
                else
                {
                    b.Append(",");
                }

                if (BearingToWayPoint.HasValue)
                {
                    b.AppendFormat(CultureInfo.InvariantCulture, "{0:F1},", BearingToWayPoint.Value.Normalize(true).Degrees);
                }
                else
                {
                    b.Append(",");
                }

                if (ApproachSpeed.HasValue)
                {
                    b.AppendFormat(CultureInfo.InvariantCulture, "{0:F1},", ApproachSpeed.Value.Knots);
                }
                else
                {
                    b.Append(",");
                }

                if (Arrived)
                {
                    // Not sure what the final D means here. My receiver sends it, but it is not documented.
                    b.Append("A,D");
                }
                else
                {
                    b.Append("V,D");
                }

                return b.ToString();
            }

            return string.Empty;
        }

        public override string ToReadableContent()
        {
            if (Valid)
            {
                if (NextWayPoint != null)
                {
                    return $"Next waypoint: {NextWayPoint}, Track deviation: {CrossTrackError}, Distance: {DistanceToWayPoint}";
                }
            }

            return "Not a valid RMB message";
        }
    }
}

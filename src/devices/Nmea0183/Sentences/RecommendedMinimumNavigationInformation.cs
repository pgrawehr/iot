﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183.Sentences
{
    // http://www.tronico.fi/OH6NT/docs/NMEA0183.pdf
    // page 14

    /// <summary>
    /// Represents RMC NMEA0183 sentence (Recommended Minimum Navigation Information)
    /// </summary>
    public class RecommendedMinimumNavigationInformation : NmeaSentence
    {
        public static SentenceId Id => new SentenceId('R', 'M', 'C');
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        public NavigationStatus? Status
        {
            get;
        }

        public GeographicPosition Position
        {
            get;
        }

        public Speed SpeedOverGround
        {
            get;
        }

        public Angle TrackMadeGoodInDegreesTrue
        {
            get;
        }

        public Angle? MagneticVariationInDegrees
        {
            get;
        }

        // http://www.tronico.fi/OH6NT/docs/NMEA0183.pdf
        // doesn't mention this field but all other sentences have this
        // and at least NEO-M8 sends it
        // possibly each status is related with some part of the message
        // but this unofficial spec does not clarify it
        public NavigationStatus? Status2 { get; private set; }

        public override string ToNmeaMessage()
        {
            // seems nullable don't interpolate well
            StringBuilder b = new StringBuilder();
            string time = DateTime.HasValue ? DateTime.Value.ToString("HHmmss.fff", CultureInfo.InvariantCulture) : string.Empty;
            b.AppendFormat($"{time},");
            string status = Status.HasValue ? $"{(char)Status}" : "V";
            b.AppendFormat($"{status},");
            double? degrees;
            CardinalDirection? direction;
            (degrees, direction) = DegreesToNmea0183(Position.Latitude, true);
            if (degrees.HasValue && direction.HasValue)
            {
                b.AppendFormat(CultureInfo.InvariantCulture, "{0:0000.00000},{1},", degrees.Value, (char)direction);
            }
            else
            {
                b.Append(",,");
            }

            (degrees, direction) = DegreesToNmea0183(Position.Longitude, false);
            if (degrees.HasValue && direction.HasValue)
            {
                b.AppendFormat(CultureInfo.InvariantCulture, "{0:00000.00000},{1},", degrees.Value, (char)direction);
            }
            else
            {
                b.Append(",,");
            }

            string speed = SpeedOverGround.Value.ToString("0.000", CultureInfo.InvariantCulture);
            b.Append($"{speed},");
            string track = TrackMadeGoodInDegreesTrue.Value.ToString("0.000", CultureInfo.InvariantCulture);
            b.Append($"{track},");
            string date = DateTime.HasValue ? DateTime.Value.ToString("ddMMyy", CultureInfo.InvariantCulture) : null;
            b.Append($"{date},");
            if (MagneticVariationInDegrees.HasValue)
            {
                string mag = Math.Abs(MagneticVariationInDegrees.Value.Value).ToString("0.000", CultureInfo.InvariantCulture);
                if (MagneticVariationInDegrees >= Angle.Zero)
                {
                    b.Append($"{mag},E");
                }
                else
                {
                    b.Append($"{mag},W");
                }
            }
            else
            {
                b.Append(",");
            }

            // undocumented status field will be optionally displayed
            if (Status2.HasValue)
            {
                b.Append($",{(char)Status2.Value}"); // Also only add the comma if the parameter exists
            }

            return b.ToString();
        }

        public RecommendedMinimumNavigationInformation(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        public RecommendedMinimumNavigationInformation(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            string newTime = ReadString(field);
            NavigationStatus? status = (NavigationStatus?)ReadChar(field);
            double? lat = ReadValue(field);
            CardinalDirection? latTurn = (CardinalDirection?)ReadChar(field);
            double? lon = ReadValue(field);
            CardinalDirection? lonTurn = (CardinalDirection?)ReadChar(field);
            double? speed = ReadValue(field);
            double? track = ReadValue(field);
            string date = ReadString(field);

            DateTimeOffset dateTime;
            if (date.Length != 0)
            {
                dateTime = ParseDateTime(date, newTime);
            }
            else
            {
                dateTime = ParseDateTime(time, newTime);
            }

            double? mag = ReadValue(field);
            CardinalDirection? magTurn = (CardinalDirection?)ReadChar(field);

            // handle undocumented field
            // per spec we should not have any extra fields but NEO-M8 does have them
            if (field.MoveNext())
            {
                string val = field.Current;
                Status2 = string.IsNullOrEmpty(val) ? (NavigationStatus?)null : (NavigationStatus?)val.FirstOrDefault();
            }

            DateTime = dateTime;
            Status = status;
            double? latitude = Nmea0183ToDegrees(lat, latTurn);
            double? longitude = Nmea0183ToDegrees(lon, lonTurn);

            if (latitude.HasValue && longitude.HasValue)
            {
                Position = new GeographicPosition(latitude.Value, longitude.Value, 0);
                // If the message contains no position, it is unusable.
                // On the other hand, if the position is known (meaning the GPS receiver works), speed and track are known, too.
                Valid = true;
            }

            SpeedOverGround = Speed.FromKnots(speed.GetValueOrDefault(0));

            if (track.HasValue)
            {
                TrackMadeGoodInDegreesTrue = Angle.FromDegrees(track.Value);
            }
            else
            {
                TrackMadeGoodInDegreesTrue = Angle.Zero;
            }

            if (mag.HasValue && magTurn.HasValue)
            {
                MagneticVariationInDegrees = Angle.FromDegrees(mag.Value * DirectionToSign(magTurn.Value));
            }
        }

        public RecommendedMinimumNavigationInformation(
            DateTimeOffset? dateTime,
            NavigationStatus? status,
            GeographicPosition position,
            Speed speedOverGround,
            Angle trackMadeGoodInDegreesTrue,
            Angle? magneticVariationInDegrees)
        : base(OwnTalkerId, Id, dateTime.GetValueOrDefault(DateTimeOffset.UtcNow))
        {
            DateTime = dateTime;
            Status = status;
            Position = position;
            SpeedOverGround = speedOverGround;
            TrackMadeGoodInDegreesTrue = trackMadeGoodInDegreesTrue;
            MagneticVariationInDegrees = magneticVariationInDegrees;
            Valid = true;
        }

        private static int DirectionToSign(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                case CardinalDirection.East:
                    return 1;
                case CardinalDirection.South:
                case CardinalDirection.West:
                    return -1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        internal static double? Nmea0183ToDegrees(double? degreesMinutes, CardinalDirection? direction)
        {
            if (!degreesMinutes.HasValue || !direction.HasValue)
            {
                return null;
            }

            // ddddmm.mm
            double degrees = Math.Floor(degreesMinutes.Value / 100);
            double minutes = degreesMinutes.Value - (degrees * 100);
            return ((double)degrees + (double)minutes / 60.0) * DirectionToSign(direction.Value);
        }

        public override string ToReadableContent()
        {
            if (Valid)
            {
                return $"Position: {Position} / Speed {SpeedOverGround} / Track {TrackMadeGoodInDegreesTrue}";
            }

            return "Position unknown";
        }

        internal static (double? degreesMinutes, CardinalDirection? direction) DegreesToNmea0183(double? degrees, bool isLatitude)
        {
            if (!degrees.HasValue)
            {
                return (null, null);
            }

            CardinalDirection? direction;
            double positiveDegrees;

            if (degrees.Value >= 0)
            {
                direction = isLatitude ? CardinalDirection.North : CardinalDirection.East;
                positiveDegrees = degrees.Value;
            }
            else
            {
                direction = isLatitude ? CardinalDirection.South : CardinalDirection.West;
                positiveDegrees = -degrees.Value;
            }

            int integerDegrees = (int)positiveDegrees;
            double fractionDegrees = positiveDegrees - integerDegrees;
            double minutes = fractionDegrees * 60;

            // ddddmm.mm
            double? degreesMinutes = integerDegrees * 100 + minutes;
            return (degreesMinutes, direction);
        }

        public enum NavigationStatus : byte
        {
            Valid = (byte)'A',
            NavigationReceiverWarning = (byte)'V',
        }
    }
}
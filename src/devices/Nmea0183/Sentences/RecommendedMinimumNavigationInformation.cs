// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183.Sentences
{
    // http://www.tronico.fi/OH6NT/docs/NMEA0183.pdf
    // page 14

    /// <summary>
    /// Represents RecommendedMinimumNavigationInformation NMEA0183 sentence
    /// </summary>
    public class RecommendedMinimumNavigationInformation : NmeaSentence
    {
        public static SentenceId Id => new SentenceId('R', 'M', 'C');
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        public NavigationStatus? Status { get; private set; }

        /// <summary>
        /// Latitude in degrees. Positive value means north, negative means south.
        /// </summary>
        /// <value>Value of latitude in degrees or null when value is not provided</value>
        public double? LatitudeDegrees
        {
            get
            {
                return Nmea0183ToDegrees(_latitude, _latitudeTurn);
            }
            private set
            {
                (_latitude, _latitudeTurn) = DegreesToNmea0183(value, isLatitude: true);
            }
        }

        private double? _latitude;
        private CardinalDirection? _latitudeTurn;

        /// <summary>
        /// Longitude in degrees. Positive value means east, negative means west.
        /// </summary>
        /// <value>Value of longitude in degrees or null when value is not provided</value>
        public double? LongitudeDegrees
        {
            get => Nmea0183ToDegrees(_longitude, _longitudeTurn);
            private set
            {
                (_longitude, _longitudeTurn) = DegreesToNmea0183(value, isLatitude: false);
            }
        }

        private double? _longitude;
        private CardinalDirection? _longitudeTurn;
        public double? SpeedOverGroundInKnots { get; private set; }

        public Speed Speed
        {
            get
            {
                return Speed.FromKnots(SpeedOverGroundInKnots.GetValueOrDefault(0));
            }
        }

        public Angle? TrackMadeGoodInDegreesTrue { get; private set; }
        public Angle? MagneticVariationInDegrees
        {
            get
            {
                if (!_positiveMagneticVariationInDegrees.HasValue || !_magneticVariationTurn.HasValue)
                {
                    return null;
                }

                return _positiveMagneticVariationInDegrees.Value * DirectionToSign(_magneticVariationTurn.Value);
            }
            private set
            {
                if (!value.HasValue)
                {
                    _positiveMagneticVariationInDegrees = null;
                    _magneticVariationTurn = null;
                    return;
                }

                if (value >= Angle.Zero)
                {
                    _positiveMagneticVariationInDegrees = value;
                    _magneticVariationTurn = CardinalDirection.East;
                }
                else
                {
                    _positiveMagneticVariationInDegrees = -value;
                    _magneticVariationTurn = CardinalDirection.West;
                }
            }
        }

        private Angle? _positiveMagneticVariationInDegrees;
        private CardinalDirection? _magneticVariationTurn;

        // http://www.tronico.fi/OH6NT/docs/NMEA0183.pdf
        // doesn't mention this field but all other sentences have this
        // and at least NEO-M8 sends it
        // possibly each status is related with some part of the message
        // but this unofficial spec does not clarify it
        public NavigationStatus? Status2 { get; private set; }

        public override string ToNmeaMessage()
        {
            // seems nullable don't interpolate well
            string time = DateTime.HasValue ? DateTime.Value.ToString("HHmmss.fff", CultureInfo.InvariantCulture) : null;
            string status = Status.HasValue ? $"{(char)Status}" : null;
            string lat = _latitude.HasValue ? _latitude.Value.ToString("0000.00000", CultureInfo.InvariantCulture) : null;
            string latTurn = _latitudeTurn.HasValue ? $"{(char)_latitudeTurn.Value}" : null;
            string lon = _longitude.HasValue ? _longitude.Value.ToString("00000.00000", CultureInfo.InvariantCulture) : null;
            string lonTurn = _longitudeTurn.HasValue ? $"{(char)_longitudeTurn.Value}" : null;
            string speed = SpeedOverGroundInKnots.HasValue ? SpeedOverGroundInKnots.Value.ToString("0.000", CultureInfo.InvariantCulture) : null;
            string track = TrackMadeGoodInDegreesTrue.HasValue ? TrackMadeGoodInDegreesTrue.Value.Value.ToString("0.000", CultureInfo.InvariantCulture) : null;
            string date = DateTime.HasValue ? DateTime.Value.ToString("ddMMyy", CultureInfo.InvariantCulture) : null;
            string mag = _positiveMagneticVariationInDegrees.HasValue ? _positiveMagneticVariationInDegrees.Value.ToString("0.000", CultureInfo.InvariantCulture) : null;
            string magTurn = _magneticVariationTurn.HasValue ? $"{(char)_magneticVariationTurn.Value}" : null;

            // undocumented status field will be optionally displayed
            string status2 = Status2.HasValue ? $",{(char)Status2}" : null;

            return FormattableString.Invariant($"{time},{status},{lat},{latTurn},{lon},{lonTurn},{speed},{track},{date},{mag},{magTurn}{status2}");
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
                Status2 = string.IsNullOrEmpty(val) ? (NavigationStatus?)null : (NavigationStatus?)val.Single();
            }

            DateTime = dateTime;
            Status = status;
            _latitude = lat;
            _latitudeTurn = latTurn;
            _longitude = lon;
            _longitudeTurn = lonTurn;
            SpeedOverGroundInKnots = speed;
            if (track.HasValue)
            {
                TrackMadeGoodInDegreesTrue = Angle.FromDegrees(track.Value);
            }
            else
            {
                TrackMadeGoodInDegreesTrue = null;
            }

            if (mag.HasValue)
            {
                _positiveMagneticVariationInDegrees = Angle.FromDegrees(mag.Value);
            }
            else
            {
                _positiveMagneticVariationInDegrees = null;
            }

            _magneticVariationTurn = magTurn;
            Valid = true; // for this message, we need to check on the individual fields
        }

        public RecommendedMinimumNavigationInformation(
            DateTimeOffset? dateTime,
            NavigationStatus? status,
            double? latitude,
            double? longitude,
            double? speedOverGroundInKnots,
            Angle? trackMadeGoodInDegreesTrue,
            Angle? magneticVariationInDegrees)
        : base(OwnTalkerId, Id, dateTime.GetValueOrDefault(DateTimeOffset.UtcNow))
        {
            DateTime = dateTime;
            Status = status;
            LatitudeDegrees = latitude;
            LongitudeDegrees = longitude;
            SpeedOverGroundInKnots = speedOverGroundInKnots;
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
            if (LatitudeDegrees.HasValue && LongitudeDegrees.HasValue)
            {
                GeographicPosition position = new GeographicPosition(LatitudeDegrees.Value, LongitudeDegrees.Value, 0);
                return $"Position: {position} / Speed {SpeedOverGroundInKnots} / Track {TrackMadeGoodInDegreesTrue}";
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

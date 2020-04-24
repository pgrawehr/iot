// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nmea0183;
using Nmea0183.Sentences;
using Units;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183.Sentences
{
    // http://www.tronico.fi/OH6NT/docs/NMEA0183.pdf
    // page 14

    /// <summary>
    /// Represents GlobalPositioningSystemFixData (GGA) NMEA0183 sentence
    /// </summary>
    public class GlobalPositioningSystemFixData : NmeaSentence
    {
        public static SentenceId Id => new SentenceId("GGA");
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        public GpsQuality Status { get; private set; }

        private double? _latitude;
        private CardinalDirection? _latitudeTurn;

        private double? _longitude;
        private CardinalDirection? _longitudeTurn;

        /// <summary>
        /// Latitude in degrees. Positive value means north, negative means south.
        /// </summary>
        /// <value>Value of latitude in degrees or null when value is not provided</value>
        public double? LatitudeDegrees
        {
            get => RecommendedMinimumNavigationInformation.Nmea0183ToDegrees(_latitude, _latitudeTurn);
        }

        /// <summary>
        /// Longitude in degrees. Positive value means east, negative means west.
        /// </summary>
        /// <value>Value of longitude in degrees or null when value is not provided</value>
        public double? LongitudeDegrees
        {
            get => RecommendedMinimumNavigationInformation.Nmea0183ToDegrees(_longitude, _longitudeTurn);
        }

        public double? Undulation
        {
            get;
        }

        public double? GeoidAltitude
        {
            get;
        }

        public double? EllipsoidAltitude
        {
            get;
        }

        /// <summary>
        /// Number of satellites in view. A maximum of 12 is reported by this message.
        /// </summary>
        public int NumberOfSatellites { get; }

        public double Hdop
        {
            get;
        }

        public GeographicPosition Position
        {
            get;
        }

        public override string ToNmeaMessage()
        {
            // seems nullable don't interpolate well
            string time = DateTime.HasValue ? $"{DateTime.Value.ToString("HHmmss.fff", CultureInfo.InvariantCulture)}" : null;
            string lat = _latitude.HasValue ? _latitude.Value.ToString("0000.00000", CultureInfo.InvariantCulture) : null;
            string latTurn = _latitudeTurn.HasValue ? $"{(char)_latitudeTurn.Value}" : null;
            string lon = _longitude.HasValue ? _longitude.Value.ToString("00000.00000", CultureInfo.InvariantCulture) : null;
            string lonTurn = _longitudeTurn.HasValue ? $"{(char)_longitudeTurn.Value}" : null;
            string quality = ((int)Status).ToString(CultureInfo.InvariantCulture);
            string numSats = NumberOfSatellites.ToString(CultureInfo.InvariantCulture);
            string geoidElevation = GeoidAltitude.HasValue
                ? GeoidAltitude.Value.ToString("F1", CultureInfo.InvariantCulture)
                : null;
            string hdop = Hdop.ToString(CultureInfo.InvariantCulture);
            string undulation = Undulation.HasValue
                ? Undulation.Value.ToString("F1", CultureInfo.InvariantCulture)
                : null;

            return $"{time},{lat},{latTurn},{lon},{lonTurn},{quality},{numSats},{hdop},{geoidElevation},M,{undulation},M,,";
        }

        public GlobalPositioningSystemFixData(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        public GlobalPositioningSystemFixData(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            string timeString = ReadString(field);
            double? lat = ReadValue(field);
            CardinalDirection? latTurn = (CardinalDirection?)ReadChar(field);
            double? lon = ReadValue(field);
            CardinalDirection? lonTurn = (CardinalDirection?)ReadChar(field);
            int? gpsStatus = ReadInt(field);

            int? numberOfSatellites = ReadInt(field);

            double? hdop = ReadValue(field);

            double? geoidHeight = ReadValue(field);

            char? unitOfHeight = ReadChar(field);

            double? undulation = ReadValue(field);

            char? unitOfUndulation = ReadChar(field);

            DateTimeOffset dateTime;
            dateTime = ParseDateTime(time, timeString);

            DateTime = dateTime;
            Status = (GpsQuality)gpsStatus.GetValueOrDefault(0);
            _latitude = lat;
            _latitudeTurn = latTurn;
            _longitude = lon;
            _longitudeTurn = lonTurn;
            Undulation = undulation;
            GeoidAltitude = geoidHeight;
            EllipsoidAltitude = geoidHeight + undulation;
            Hdop = hdop.GetValueOrDefault(99);
            NumberOfSatellites = numberOfSatellites.GetValueOrDefault(0);

            if (_latitude != null && _longitude != null && GeoidAltitude != null && Undulation != null &&
                unitOfHeight.HasValue && unitOfUndulation.HasValue)
            {
                Valid = true;
                double latitudeDegrees = RecommendedMinimumNavigationInformation.Nmea0183ToDegrees(_latitude, _latitudeTurn).GetValueOrDefault(0);
                double longitudeDegrees = RecommendedMinimumNavigationInformation.Nmea0183ToDegrees(_longitude, _longitudeTurn).GetValueOrDefault(0);
                Position = new GeographicPosition(latitudeDegrees, longitudeDegrees, EllipsoidAltitude.GetValueOrDefault(0));
            }
            else if (_latitude != null && _longitude != null)
            {
                Valid = true;
                double latitudeDegrees = RecommendedMinimumNavigationInformation.Nmea0183ToDegrees(_latitude, _latitudeTurn).GetValueOrDefault(0);
                double longitudeDegrees = RecommendedMinimumNavigationInformation.Nmea0183ToDegrees(_longitude, _longitudeTurn).GetValueOrDefault(0);
                Position = new GeographicPosition(latitudeDegrees, longitudeDegrees, 0);
                EllipsoidAltitude = GeoidAltitude = Undulation = null;
            }
            else
            {
                // No improvement over RMC if these are not all valid
                Valid = false;
                Position = new GeographicPosition(); // Invalid, but not null
            }
        }

        public GlobalPositioningSystemFixData(
            DateTimeOffset? dateTime,
            GpsQuality status,
            GeographicPosition position,
            double? geoidAltitude,
            double? ellipsoidAltitude,
            double hdop,
            int numberOfSatellites)
        : base(OwnTalkerId, Id, dateTime.GetValueOrDefault(DateTimeOffset.UtcNow))
        {
            DateTime = dateTime;
            Status = status;
            position = position.NormalizeAngleTo180();
            (_latitude, _latitudeTurn) = RecommendedMinimumNavigationInformation.DegreesToNmea0183(position.Latitude, true);
            (_longitude, _longitudeTurn) = RecommendedMinimumNavigationInformation.DegreesToNmea0183(position.Longitude, false);
            EllipsoidAltitude = ellipsoidAltitude;
            GeoidAltitude = geoidAltitude;
            Undulation = EllipsoidAltitude - geoidAltitude;
            Hdop = hdop;
            NumberOfSatellites = numberOfSatellites;
        }

        public override string ToReadableContent()
        {
            if (LatitudeDegrees.HasValue && LongitudeDegrees.HasValue && EllipsoidAltitude.HasValue)
            {
                GeographicPosition position = new GeographicPosition(LatitudeDegrees.Value, LongitudeDegrees.Value, EllipsoidAltitude.Value);
                return $"Position: {position}";
            }

            return "Position unknown";
        }
    }
}

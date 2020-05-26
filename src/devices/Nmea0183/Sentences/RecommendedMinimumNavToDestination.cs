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
            string nextWayPointHemisphere = ReadString(field);
            double? nextWayPointLongitude = ReadValue(field);
            string nextWayPointDirection = ReadString(field);
            double? rangeToWayPoint = ReadValue(field);
            double? bearing = ReadValue(field);
            double? approachSpeed = ReadValue(field);
            string arrivalStatus = ReadString(field);

            if (overallStatus == "A")
            {
                CrossTrackError = Length.FromNauticalMiles(crossTrackError.GetValueOrDefault(0));
            }
        }

        public RecommendedMinimumNavToDestination(
            DateTimeOffset? dateTime,
            double? latitude,
            double? longitude,
            double? speedOverGroundInKnots,
            Angle? trackMadeGoodInDegreesTrue,
            Angle? magneticVariationInDegrees)
        : base(OwnTalkerId, Id, dateTime.GetValueOrDefault(DateTimeOffset.UtcNow))
        {
            Valid = true;
        }

        public Length CrossTrackError
        {
            get;
        }

        public override string ToNmeaMessage()
        {
            return string.Empty;
        }

        public override string ToReadableContent()
        {
            return string.Empty;
        }
    }
}

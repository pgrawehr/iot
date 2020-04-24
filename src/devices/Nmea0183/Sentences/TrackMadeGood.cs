using System;
using System.Collections.Generic;
using System.Text;
using Iot.Device.Nmea0183;
using Units;

namespace Nmea0183.Sentences
{
    /// <summary>
    /// HDT Sentence: Heading true.
    /// This is either a calculated message by using the HDT message and a magnetic variation model, or directly measured using a gyrocompass.
    /// But since these are very expensive an power-hungry, they are only available in big ships or aircraft.
    /// </summary>
    public class TrackMadeGood : NmeaSentence
    {
        /// <summary>
        /// This sentence's id
        /// </summary>
        public static SentenceId Id => new SentenceId("VTG");
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        /// <summary>
        /// Constructs a new MWV sentence
        /// </summary>
        public TrackMadeGood(Angle courseOverGroundTrue, Angle? courseOverGroundMagnetic, Speed speed)
            : base(OwnTalkerId, Id, DateTimeOffset.UtcNow)
        {
            CourseOverGroundTrue = courseOverGroundTrue;
            CourseOverGroundMagnetic = courseOverGroundMagnetic;
            Speed = speed;
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        public TrackMadeGood(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Date and time message (ZDA). This should not normally need the last time as argument, because it defines it.
        /// </summary>
        public TrackMadeGood(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            double? courseOverGroundTrue = ReadValue(field);
            string reference = ReadString(field) ?? string.Empty;

            double? courseOverGroundMagnetic = ReadValue(field);
            string reference2 = ReadString(field) ?? string.Empty;

            double? speedKnots = ReadValue(field);
            string reference3 = ReadString(field) ?? string.Empty;

            // The HDT sentence must have a "T" (True) reference, otherwise something is fishy
            if (reference == "T" && courseOverGroundTrue.HasValue)
            {
                CourseOverGroundTrue = Angle.FromDegrees(courseOverGroundTrue.Value);
            }

            if (reference2 == "M" && courseOverGroundMagnetic.HasValue)
            {
                CourseOverGroundMagnetic = Angle.FromDegrees(courseOverGroundMagnetic.Value);
            }

            if (reference3 == "N" && speedKnots.HasValue)
            {
                Speed = Speed.FromKnots(speedKnots.Value);
            }

            // At least these should be set
            Valid = courseOverGroundTrue.HasValue && speedKnots.HasValue;
        }

        /// <summary>
        /// Course over ground, degrees true
        /// </summary>
        public Angle CourseOverGroundTrue
        {
            get;
            set;
        }

        /// <summary>
        /// Course over ground, degrees magnetic.
        /// Note: Usually derived from <see cref="CourseOverGroundTrue"/>, the current position and a magnetic variation model.
        /// </summary>
        public Angle? CourseOverGroundMagnetic
        {
            get;
            set;
        }

        /// <summary>
        /// Speed over ground
        /// </summary>
        public Speed Speed
        {
            get;
            set;
        }

        /// <summary>
        /// Presents this message as output
        /// </summary>
        public override string ToString()
        {
            if (Valid)
            {
                double? magnetic = CourseOverGroundMagnetic.HasValue ? CourseOverGroundMagnetic.Value.Degrees : (double?)null;
                return $"{CourseOverGroundTrue.Degrees:F1},T,{magnetic:F1},M,{Speed.Knots:F1},N,{Speed.KilometersPerHour:F1},K,A";
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override string ToReadableContent()
        {
            if (Valid)
            {
                return $"Course over ground true: {CourseOverGroundTrue.Degrees:F1}, Speed: {Speed.Knots:F1} kts";
            }

            return "Course unknown";
        }
    }
}

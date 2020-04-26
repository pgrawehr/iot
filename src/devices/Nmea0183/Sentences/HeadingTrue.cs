using System;
using System.Collections.Generic;
using System.Text;
using Iot.Device.Nmea0183;
using Units;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// HDT Sentence: Heading true.
    /// This is either a calculated message by using the HDT message and a magnetic variation model, or directly measured using a gyrocompass.
    /// But since these are very expensive an power-hungry, they are only available in big ships or aircraft.
    /// </summary>
    public class HeadingTrue : NmeaSentence
    {
        /// <summary>
        /// This sentence's id
        /// </summary>
        public static SentenceId Id => new SentenceId("HDT");
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        /// <summary>
        /// Constructs a new MWV sentence
        /// </summary>
        public HeadingTrue(double angle)
            : base(OwnTalkerId, Id, DateTimeOffset.UtcNow)
        {
            Angle = angle;
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        public HeadingTrue(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Date and time message (ZDA). This should not normally need the last time as argument, because it defines it.
        /// </summary>
        public HeadingTrue(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            double? angle = ReadValue(field);
            string reference = ReadString(field) ?? string.Empty;

            // The HDT sentence must have a "T" (True) reference, otherwise something is fishy
            if (reference == "T" && angle.HasValue)
            {
                Angle = angle.Value;
                Valid = true;
            }
            else
            {
                Angle = 0;
                Valid = false;
            }
        }

        /// <summary>
        /// Angle of the wind
        /// </summary>
        public double Angle
        {
            get;
            private set;
        }

        /// <summary>
        /// Presents this message as output
        /// </summary>
        public override string ToNmeaMessage()
        {
            if (Valid)
            {
                return $"{Angle:F1},T";
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override string ToReadableContent()
        {
            if (Valid)
            {
                return $"True Heading: {Angle:F1}°";
            }

            return "True heading unknown";
        }
    }
}

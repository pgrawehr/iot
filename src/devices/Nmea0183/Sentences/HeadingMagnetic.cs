using System;
using System.Collections.Generic;
using System.Text;
using Iot.Device.Nmea0183;
using Units;

namespace Nmea0183.Sentences
{
    /// <summary>
    /// HDT Sentence: Heading magnetic.
    /// Usually measured using an electronic compass.
    /// </summary>
    public class HeadingMagnetic : NmeaSentence
    {
        /// <summary>
        /// This sentence's id
        /// </summary>
        public static SentenceId Id => new SentenceId("HDM");
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        /// <summary>
        /// Constructs a new MWV sentence
        /// </summary>
        public HeadingMagnetic(double angle)
            : base(Id)
        {
            Angle = angle;
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        public HeadingMagnetic(TalkerSentence sentence, DateTimeOffset time)
            : this(Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Date and time message (ZDA). This should not normally need the last time as argument, because it defines it.
        /// </summary>
        public HeadingMagnetic(IEnumerable<string> fields, DateTimeOffset today)
            : base(Id)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            double? angle = ReadValue(field);
            string reference = ReadString(field) ?? string.Empty;

            // The HDM sentence must have a "M" (Magnetic) reference, otherwise something is fishy
            if (reference == "TM" && angle.HasValue)
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
        public override string ToString()
        {
            if (Valid)
            {
                return $"{Angle:F1},M";
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override string ToReadableContent()
        {
            if (Valid)
            {
                return $"Magnetic Heading: {Angle:F1}°";
            }

            return "Magnetic Heading unknown";
        }
    }
}

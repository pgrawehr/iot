using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnitsNet;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// HDG Sentence (Heading, deviation, variation)
    /// Usually measured using an electronic compass. This is required for HDT and HDM to work properly sometimes.
    /// </summary>
    public class HeadingAndDeviation : NmeaSentence
    {
        /// <summary>
        /// This sentence's id
        /// </summary>
        public static SentenceId Id => new SentenceId("HDG");
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        /// <summary>
        /// Constructs a new MWV sentence
        /// </summary>
        public HeadingAndDeviation(Angle headingTrue, Angle? deviation, Angle? variation)
            : base(OwnTalkerId, Id, DateTimeOffset.UtcNow)
        {
            HeadingTrue = headingTrue;
            Deviation = deviation;
            Variation = variation;
            Valid = true;
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        public HeadingAndDeviation(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Magnetic heading message
        /// </summary>
        public HeadingAndDeviation(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            double? heading = ReadValue(field);
            double? deviation = ReadValue(field);
            string deviationDirection = ReadString(field);
            double? magVar = ReadValue(field);
            string magVarDirection = ReadString(field);
            string reference = ReadString(field) ?? string.Empty;

            Valid = false;
            if (heading.HasValue)
            {
                HeadingTrue = Angle.FromDegrees(heading.Value);
                // This one needs to be there, the others are optional
                Valid = true;
            }

            if (deviation.HasValue)
            {
                if (deviationDirection == "E")
                {
                    Deviation = Angle.FromDegrees(deviation.Value);
                }
                else
                {
                    Deviation = Angle.FromDegrees(deviation.Value * -1);
                }
            }

            if (magVar.HasValue)
            {
                if (magVarDirection == "E")
                {
                    Variation = Angle.FromDegrees(magVar.Value);
                }
                else
                {
                    Variation = Angle.FromDegrees(magVar.Value * -1);
                }
            }
        }

        /// <summary>
        /// Angle of the wind
        /// </summary>
        public Angle HeadingTrue
        {
            get;
            private set;
        }

        /// <summary>
        /// Deviation at current location. Usually unknown (empty)
        /// </summary>
        public Angle? Deviation
        {
            get;
            private set;
        }

        /// <summary>
        /// Magnetic variation at current location. Usually derived from the NOAA magnetic field model by one
        /// of the attached devices.
        /// </summary>
        public Angle? Variation
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
                StringBuilder b = new StringBuilder();
                b.AppendFormat(CultureInfo.InvariantCulture, "{0:F1},", HeadingTrue.Normalize(true).Degrees);
                if (Deviation.HasValue)
                {
                    b.AppendFormat(CultureInfo.InvariantCulture, "{0:F1},{1},", Math.Abs(Deviation.Value.Degrees),
                        Deviation.Value.Degrees >= 0 ? "E" : "W");
                }
                else
                {
                    b.AppendFormat(",,");
                }

                if (Variation.HasValue)
                {
                    b.AppendFormat(CultureInfo.InvariantCulture, "{0:F1},{1}", Math.Abs(Variation.Value.Degrees),
                        Variation.Value.Degrees >= 0 ? "E" : "W");
                }
                else
                {
                    b.AppendFormat(",");
                }

                return b.ToString();
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override string ToReadableContent()
        {
            if (Valid && Variation.HasValue)
            {
                return $"Magnetic Heading: {HeadingTrue.Degrees:F1}°, Variation: {Variation.Value.Degrees:F1}°";
            }

            return "Magnetic Heading unknown";
        }
    }
}

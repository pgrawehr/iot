using System;
using System.Collections.Generic;
using System.Text;
using Units;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// MWV sentence: Wind speed and wind angle (true or relative)
    /// </summary>
    public class WindSpeedAndAngle : NmeaSentence
    {
        /// <summary>
        /// This sentence's id
        /// </summary>
        public static SentenceId Id => new SentenceId("MWV");
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        /// <summary>
        /// Constructs a new MWV sentence
        /// </summary>
        public WindSpeedAndAngle(Angle angle, Speed speed, bool relative)
            : base(OwnTalkerId, Id, DateTimeOffset.UtcNow)
        {
            Speed = speed;
            Relative = relative;
            Angle = angle;
            Valid = true;
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        public WindSpeedAndAngle(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Date and time message (ZDA). This should not normally need the last time as argument, because it defines it.
        /// </summary>
        public WindSpeedAndAngle(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            double? angle = ReadValue(field);
            string reference = ReadString(field) ?? string.Empty;
            double? speed = ReadValue(field);

            string unit = ReadString(field) ?? string.Empty;
            string status = ReadString(field) ?? string.Empty;

            // Other units than "N" (knots) not supported
            if (status == "A" && angle.HasValue && speed.HasValue && unit == "N")
            {
                Angle = Angle.FromDegrees(angle.Value);
                Speed = Speed.FromKnots(speed.Value);
                if (reference == "T")
                {
                    Relative = false;
                    Angle = Angle.Normalize(true);
                }
                else
                {
                    // Default, since that's what the actual wind instrument delivers
                    Relative = true;
                    // Relative angles are +/- 180
                    Angle = Angle.Normalize(false);
                }

                Valid = true;
            }
            else
            {
                Angle = Angle.Zero;
                Speed = new Speed();
                Valid = false;
            }
        }

        /// <summary>
        /// Angle of the wind
        /// </summary>
        public Angle Angle
        {
            get;
            private set;
        }

        /// <summary>
        /// Wind speed
        /// </summary>
        public Speed Speed
        {
            get;
            private set;
        }

        /// <summary>
        /// True if the values are relative, false if they are absolute
        /// </summary>
        public bool Relative
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
                // It seems that angles should always be written 0..360.
                var normalized = Angle.Normalize(true);
                return $"{normalized.Degrees:F1},{(Relative ? "R" : "T")},{Speed.Knots:F1},N,A";
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override string ToReadableContent()
        {
            if (Valid)
            {
                if (Relative)
                {
                    return $"Apparent wind direction: {Angle.Degrees:F1}° Speed: {Speed.Knots:F1}Kts";
                }
                else
                {
                    return $"Absolute wind direction: {Angle.Degrees:F1}° Speed: {Speed.Knots:F1}Kts";
                }
            }

            return "Wind speed/direction unknown";
        }
    }
}

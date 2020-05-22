using System;
using System.Collections.Generic;
using System.Text;
using Iot.Device.Nmea0183;
using Units;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// DBS sentence: Depth below surface (sent by depth transducer if configured properly)
    /// </summary>
    public class DepthBelowSurface : NmeaSentence
    {
        /// <summary>
        /// This sentence's id
        /// </summary>
        public static SentenceId Id => new SentenceId("DBS");
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        /// <summary>
        /// Constructs a new MWV sentence
        /// </summary>
        public DepthBelowSurface(Distance depth)
            : base(OwnTalkerId, Id, DateTimeOffset.UtcNow)
        {
            Depth = depth;
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        public DepthBelowSurface(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Date and time message (ZDA). This should not normally need the last time as argument, because it defines it.
        /// </summary>
        public DepthBelowSurface(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            string feet = ReadString(field);
            string feetUnit = ReadString(field);
            double? meters = ReadValue(field);
            string metersUnit = ReadString(field);

            if (metersUnit == "M" && meters.HasValue)
            {
                Depth = Distance.FromMeters(meters.Value);
                Valid = true;
            }
            else
            {
                Depth = Distance.Zero;
                Valid = false;
            }
        }

        /// <summary>
        /// Cross track distance, meters
        /// </summary>
        public Distance Depth
        {
            get;
            private set;
        }

        /// <summary>
        /// Direction to steer.
        /// True: Left, False: Right
        /// </summary>
        public bool Left
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
                return FormattableString.Invariant($"{Depth.Feet:F1},f,{Depth.Meters:F2},M,{Depth.Fathoms:F1},F");
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override string ToReadableContent()
        {
            if (Valid)
            {
                return $"Depth below surface: {Depth.Meters:F2}m";
            }

            return "No valid depth";
        }
    }
}

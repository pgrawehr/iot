using System;
using System.Collections.Generic;
using System.Text;
using Iot.Device.Nmea0183;
using Units;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// XTE sentence: Cross track error (one of the most important messages used to control autopilot)
    /// </summary>
    public class CrossTrackError : NmeaSentence
    {
        /// <summary>
        /// This sentence's id
        /// </summary>
        public static SentenceId Id => new SentenceId("XTE");
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        /// <summary>
        /// Constructs a new MWV sentence
        /// </summary>
        public CrossTrackError(Distance distance, bool left)
            : base(OwnTalkerId, Id, DateTimeOffset.UtcNow)
        {
            Distance = distance;
            Left = left;
            Valid = true;
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        public CrossTrackError(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Date and time message (ZDA). This should not normally need the last time as argument, because it defines it.
        /// </summary>
        public CrossTrackError(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            string status1 = ReadString(field);
            string status2 = ReadString(field);
            double? distance = ReadValue(field);
            string direction = ReadString(field);
            string unit = ReadString(field);

            if (status1 == "A" && status2 == "A" && distance.HasValue && (direction == "L" || direction == "R") && unit == "N")
            {
                Distance = Distance.FromNauticalMiles(distance.Value);
                if (direction == "L")
                {
                    Left = true;
                }
                else
                {
                    Left = false;
                }

                Valid = true;
            }
            else
            {
                Distance = Distance.Zero;
                Valid = false;
            }
        }

        /// <summary>
        /// Cross track distance, meters
        /// </summary>
        public Distance Distance
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
                return FormattableString.Invariant($"A,A,{Distance.NauticalMiles:F3},{(Left ? "L" : "R")},N,D");
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override string ToReadableContent()
        {
            if (Valid)
            {
                if (Left)
                {
                    return $"The route is {Distance.NauticalMiles:F3} to the left";
                }
                else
                {
                    return $"The route is {Distance.NauticalMiles:F3} to the right";
                }
            }

            return "No valid direction to route";
        }
    }
}

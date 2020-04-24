using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iot.Device.Nmea0183;

namespace Nmea0183.Sentences
{
    /// <summary>
    /// This sentence type is used if no better matching sentence type could be found. This allows forwarding of messages even if we don't need/understand them.
    /// </summary>
    public class UnknownSentence : NmeaSentence
    {
        private string[] _fields;

        /// <summary>
        /// Creates an unknown sentence from a split of parameters
        /// </summary>
        public UnknownSentence(TalkerId talkerId, SentenceId id, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, id, time)
        {
            _fields = fields.ToArray();
            Valid = true;
        }

        /// <summary>
        /// Returns the formatted payload
        /// </summary>
        public override string ToString()
        {
            return string.Join(',', _fields);
        }

        /// <inheritdoc />
        public override string ToReadableContent()
        {
            return $"${TalkerId}{SentenceId},{ToString()}"; // Cannot do much else here
        }
    }
}

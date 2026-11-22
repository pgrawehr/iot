using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Nmea0183;
using Iot.Device.Nmea0183.Sentences;

namespace Iot.Device.Marine.Sentences.Nmea0183
{
    /// <summary>
    /// This is an abstract class that does not contain any implementation. It is used to identify NMEA 0183 sentences.
    /// </summary>
    public abstract class Nmea0183Sentence : NmeaSentence
    {
        /// <summary>
        /// A nmea 0183 sentence is identified by a talker id and a sentence id.
        /// The talker id identifies the source of the sentence, while the sentence id identifies the type of sentence.
        /// </summary>
        /// <param name="talker">Talker ID, 2 letters</param>
        /// <param name="id">Sentence ID, 3 letters</param>
        /// <param name="time">Timestamp of the sentence</param>
        protected Nmea0183Sentence(TalkerId talker, SentenceId id, DateTimeOffset time)
            : base(talker, id, time)
        {
        }
    }
}

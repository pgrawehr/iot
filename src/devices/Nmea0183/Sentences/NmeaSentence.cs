using System;
using System.Collections.Generic;
using System.Text;
using Iot.Device.Nmea0183;

namespace Nmea0183.Sentences
{
    /// <summary>
    /// Base class for Nmea Sentences
    /// </summary>
    public abstract class NmeaSentence
    {
        /// <summary>
        /// Constructs an instance of this abstract class
        /// </summary>
        /// <param name="id">Sentence Id</param>
        protected NmeaSentence(SentenceId id)
        {
            SentenceId = id;
        }

        /// <summary>
        /// The sentence Id of this packet
        /// </summary>
        public SentenceId SentenceId
        {
            get;
        }
    }
}

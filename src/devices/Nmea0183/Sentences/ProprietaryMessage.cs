// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using UnitsNet;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// Proprietary message used to pass NMEA2000 messages over NMEA0183, only supported
    /// by some converters and for some messages, for instance engine parameters.
    /// The messages are usually not fully documented, but the SeaSmart (v1.6.0) protocol
    /// specification may help (and some trying around)
    /// </summary>
    public abstract class ProprietaryMessage : NmeaSentence
    {
        /// <summary>
        /// This sentence's id
        /// </summary>
        public static SentenceId Id => new SentenceId("DIN");
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        /// <summary>
        /// Creates a default message of this type
        /// </summary>
        protected ProprietaryMessage()
            : base(TalkerId.Proprietary, Id, DateTimeOffset.UtcNow)
        {
        }
    }
}

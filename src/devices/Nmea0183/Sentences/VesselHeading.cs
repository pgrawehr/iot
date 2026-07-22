// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183.Sentences
{
    public class VesselHeading : Nmea2000PackedMessage
    {
        /// <summary>
        /// Hexadecimal identifier for this message
        /// </summary>
        public const int HexId = 0x1F112;

        public override int Identifier => HexId;

        public Angle Heading
        {
            get;
            set;
        }

        public Angle? Deviation
        {
            get;
            set;
        }

        public Angle? Variation
        {
            get;
            set;
        }

        public bool IsMagnetic
        {
            get;
            set;
        }

        public override string ToReadableContent()
        {
            return $"Vessel {(IsMagnetic ? "Magnetic" : "True")} Heading: {Heading} Variation: {Variation}";
        }

        public VesselHeading(Angle heading, bool magnetic)
        {
            Heading = heading;
            IsMagnetic = magnetic;
            Valid = true;
        }

        /// <summary>
        /// Create a message object from a sentence
        /// </summary>
        /// <param name="sentence">The sentence</param>
        /// <param name="time">The current time</param>
        public VesselHeading(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Creates a message object from a decoded sentence
        /// </summary>
        /// <param name="talkerId">The source talker id</param>
        /// <param name="fields">The parameters</param>
        /// <param name="time">The current time</param>
        public VesselHeading(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            ParseCommonFields(field);

            string data = ReadString(field);

            Valid = true;
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override bool ReplacesOlderInstance => true;

        public override string ToNmeaParameterList()
        {
            string data = "FF"; // SID
            int a = (int)(Heading.Radians * 10000);
            string angle = a.ToString("X4", CultureInfo.InvariantCulture);
            angle = angle.Substring(2, 2) + angle.Substring(0, 2);
            return base.ToNmeaParameterList() + data + angle + "FFFFFFFF" + (IsMagnetic ? "FD" : "FC");
        }
    }
}

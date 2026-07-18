// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// This is a very versatile message of the Nmea2000 protocol. Depending on the function code
    /// (field 1) it can have many different meanings.
    /// </summary>
    public class GroupFunctionMessage : Nmea2000PackedMessage
    {
        // This message is usually addressed, so the last byte of the Id is the destination address (0xFF for
        // broadcast)
        public const int HexId = 0x1ED00;

        public GroupFunctionMessage(GroupFunction function)
        {
            Function = function;
            Valid = true;
        }

        /// <summary>
        /// Create a message object from a sentence
        /// </summary>
        /// <param name="sentence">The sentence</param>
        /// <param name="time">The current time</param>
        public GroupFunctionMessage(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Creates a message object from a decoded sentence
        /// </summary>
        /// <param name="talkerId">The source talker id</param>
        /// <param name="fields">The parameters</param>
        /// <param name="time">The current time</param>
        public GroupFunctionMessage(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            // We expect the fast-packet message to be decoded into a single frame here already
            IEnumerator<string> field = fields.GetEnumerator();

            ParseCommonFields(field);

            string data = ReadString(field);

            if (ReadFromHexString(data, 0, 2, false, out int f))
            {
                Function = (GroupFunction)f;
            }

            if (ReadFromHexString(data, 2, 6, true, out int pgn))
            {
                Pgn = (uint)pgn;
            }

            TransmissionInterval = null;
            if (ReadFromHexString(data, 8, 8, true, out int interval))
            {
                // -1 is "Once" and -2 is "Reset to default" (which I have never observed as being in use)
                if (interval > 0)
                {
                    TransmissionInterval = TimeSpan.FromMilliseconds(interval);
                }
            }

            TransmissionOffset = null;
            if (ReadFromHexString(data, 16, 4, true, out int offset))
            {
                if (interval > 0)
                {
                    TransmissionOffset = TimeSpan.FromMilliseconds(interval);
                }
            }

            NumberOfArguments = 0;
            if (ReadFromHexString(data, 20, 2, true, out int argCnt))
            {
                NumberOfArguments = argCnt;
            }

            Valid = true;
        }

        public int NumberOfArguments { get; set; }

        public GroupFunction Function
        {
            get;
            set;
        }

        public uint Pgn { get; set; }

        public TimeSpan? TransmissionInterval
        {
            get;
            set;
        }

        public TimeSpan? TransmissionOffset
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override int Identifier => HexId;

        /// <summary>
        /// This is true for this message.
        /// </summary>
        public override bool IsAddressed => true;

        public override string ToReadableContent()
        {
            throw new NotImplementedException();
        }
    }
}

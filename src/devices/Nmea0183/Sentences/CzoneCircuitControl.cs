// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Common;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// Sent by the plotter to toggle the state of a switch on a CZone board.
    /// </summary>
    public sealed class CzoneCircuitControl : Nmea2000PackedMessage
    {
        private readonly byte _dipSwitch;
        private string _payload; // Temporarily store the payload for later use

        /// <summary>
        /// Hexadecimal identifier for this message
        /// </summary>
        public const int HexId = 0xFF00;

        /// <summary>
        /// The Dip switch value for this board. Default for the first board is 0x80.
        /// </summary>
        public byte DipSwitch => _dipSwitch;

        /// <inheritdoc/>
        public override bool ReplacesOlderInstance => true;

        /// <summary>
        /// Creates an empty instance of this class. Can be updated later.
        /// </summary>
        /// <param name="dipSwitch">Dip switch value for this board (0-252)</param>
        public CzoneCircuitControl(byte dipSwitch)
        {
            _dipSwitch = dipSwitch;
            _payload = string.Empty;
        }

        /// <summary>
        /// Create a message object from a sentence
        /// </summary>
        /// <param name="sentence">The sentence</param>
        /// <param name="time">The current time</param>
        public CzoneCircuitControl(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Creates a message object from a decoded sentence
        /// </summary>
        /// <param name="talkerId">The source talker id</param>
        /// <param name="fields">The parameters</param>
        /// <param name="time">The current time</param>
        public CzoneCircuitControl(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            ParseCommonFields(field);

            string data = ReadString(field);

            _payload = data;
            Valid = true;
        }

        /// <inheritdoc/>
        public override string ToNmeaParameterList()
        {
            string manufacturer = WriteManufacturerAndIndustryToHex(ManufacturerCode.BepMarine2, IndustryCode.Marine);
            string instance = WriteByteToHex(_dipSwitch);
            return base.ToNmeaParameterList() + _payload;
        }

        /// <inheritdoc/>
        public override string ToReadableContent()
        {
            return $"Switch control message: {_payload}";
        }

        /// <inheritdoc/>
        public override uint Identifier => HexId;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// Special NMEA0183 message used to pass NMEA2000 messages over NMEA0183, only supported
    /// by some converters and for some messages. We also use it if we have a raw NMEA2000 input interface.
    /// The messages are usually not fully documented, but the SeaSmart (v1.6.0) protocol
    /// specification may help (and some trying around)
    /// </summary>
    public abstract class Nmea2000PackedMessage : NmeaSentence
    {
        /// <summary>
        /// This sentence's id
        /// </summary>
        public static SentenceId Id => new SentenceId("DIN");
        private static bool Matches(SentenceId sentence) => Id == sentence;

        /// <summary>
        /// Checks this message has the correct talker id
        /// </summary>
        /// <param name="sentence">The sentence to check</param>
        /// <returns>True if this input sentence matches this message type (but be careful that this message
        /// type needs further division by arguments)</returns>
        protected static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        /// <summary>
        /// Creates a default message of this type
        /// </summary>
        protected Nmea2000PackedMessage()
            : base(TalkerId.Proprietary, Id, DateTimeOffset.UtcNow)
        {
        }

        /// <summary>
        /// Used to create a message while decoding, see base class implementation
        /// </summary>
        protected Nmea2000PackedMessage(TalkerId talker, SentenceId id, DateTimeOffset time)
            : base(talker, id, time)
        {
        }

        /// <summary>
        /// The hex identifier of this message type (first field of a PCDIN message)
        /// </summary>
        public abstract int Identifier
        {
            get;
        }

        /// <summary>
        /// The timestamp for the NMEA 2000 message
        /// </summary>
        public int MessageTimeStamp
        {
            get;
            protected set;
        }

        /// <summary>
        /// The source identifier of the device which sent this message
        /// </summary>
        public uint MessageSource
        {
            get;
            protected set;
        }

        /// <summary>
        /// Reverses the endianess of an integer
        /// </summary>
        public static UInt32 InverseEndianness(UInt32 value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }

        /// <summary>
        /// Reverses the endianess of an integer
        /// </summary>
        public static Int32 InverseEndianness(Int32 value)
        {
            return (int)((value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24);
        }

        /// <summary>
        /// Decodes a value from a longer hex string (PRDIN messages contain one blob of stringly-typed hex numbers)
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="start">Start offset of required number, in nibbles(!)</param>
        /// <param name="length">Length of required number, in nibbles(!). Must be 2, 4 or 8</param>
        /// <param name="inverseEndianness">True to inverse the endianness of the number (reverse the partial string)</param>
        /// <param name="value">The output value</param>
        /// <returns>True on success, false otherwise</returns>
        /// <exception cref="ArgumentException">Length is not 2, 4 or 8</exception>
        /// <remarks>
        /// Other erroneous inputs don't throw an exception but return false, e.g. string shorter than expected or
        /// value is not a hex number. This is to prevent an exception in case of a malformed message.
        /// The offset and length are given in nibbles (half-bytes); as they operate on the input string.
        /// </remarks>
        protected bool ReadFromHexString(string input, int start, int length, bool inverseEndianness, out int value)
        {
            if (length != 2 && length != 4 && length != 8)
            {
                throw new ArgumentException("Length must be 2, 4, or 8", nameof(length));
            }

            if (input.Length < start + length)
            {
                value = 0;
                return false;
            }

            // length is given in characters here, not in bytes
            string part = input.Substring(start, length);

            if (!UInt32.TryParse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out UInt32 result))
            {
                value = 0;
                return false;
            }

            if (length == 8 && inverseEndianness)
            {
                result = InverseEndianness(result);
            }
            else if (length == 4 && inverseEndianness)
            {
                result = result >> 8 | ((result & 0xFF) << 8);
            }

            value = (int)result;
            return true;
        }

        /// <summary>
        /// Helper method for parsing the header fields (PGN, timestamp and source address)
        /// </summary>
        /// <param name="field">The enumerator over the arguments</param>
        protected void ParseCommonFields(IEnumerator<string> field)
        {
            string subMessage = ReadString(field);
            if (!int.TryParse(subMessage, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int result) || result != Identifier)
            {
                Valid = false;
                return;
            }

            string timeStamp = ReadString(field);

            if (Int32.TryParse(timeStamp, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int time1))
            {
                MessageTimeStamp = time1;
            }

            string source = ReadString(field);
            if (uint.TryParse(source, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint src))
            {
                MessageSource = src;
            }
        }

        /// <summary>
        /// This prepares the header part of the NMEA2000 PCDIN message (first 3 fields)
        /// </summary>
        public override string ToNmeaParameterList()
        {
            string pgn = Identifier.ToString("X6", CultureInfo.InvariantCulture);
            string timeStampText = MessageTimeStamp.ToString("X8", CultureInfo.InvariantCulture);
            string source = MessageSource.ToString("X2", CultureInfo.InvariantCulture);
            return $"{pgn},{timeStampText},{source},";
        }
    }
}

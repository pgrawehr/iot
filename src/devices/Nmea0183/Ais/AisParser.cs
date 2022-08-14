// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Iot.Device.Nmea0183.Sentences;

namespace Iot.Device.Nmea0183.Ais
{
    public class AisParser
    {
        public bool ThrowOnUnknownMessage { get; }
        public static SentenceId VdoId = new SentenceId("VDO");
        public static SentenceId VdmId = new SentenceId("VDM");

        private readonly PayloadDecoder _payloadDecoder;
        private readonly AisMessageFactory _messageFactory;
        private readonly PayloadEncoder _payloadEncoder;
        private readonly IDictionary<int, List<string>> _fragments = new Dictionary<int, List<string>>();

        public AisParser()
            : this(false)
        {
        }

        public AisParser(bool throwOnUnknownMessage)
            : this(new PayloadDecoder(), new AisMessageFactory(), new PayloadEncoder(), throwOnUnknownMessage)
        {
        }

        public AisParser(PayloadDecoder payloadDecoder, AisMessageFactory messageFactory, PayloadEncoder payloadEncoder, bool throwOnUnknownMessage)
        {
            ThrowOnUnknownMessage = throwOnUnknownMessage;
            _payloadDecoder = payloadDecoder;
            _messageFactory = messageFactory;
            _payloadEncoder = payloadEncoder;
        }

        /// <summary>
        /// Decode an AIS sentence from a raw NMEA0183 string, with data verification.
        /// </summary>
        /// <param name="sentence">The sentence to decode</param>
        /// <returns>An AIS message or null if the message is valid, but unrecognized</returns>
        /// <exception cref="ArgumentNullException">Sentence is null</exception>
        /// <exception cref="AisParserException">The message is syntactically incorrect</exception>
        /// <exception cref="FormatException">The message has a valid checksum, but is otherwise messed up</exception>
        public AisMessage? Parse(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                throw new ArgumentNullException(nameof(sentence));
            }

            if (sentence[0] != '!')
            {
                throw new AisParserException("Invalid sentence: sentence must start with !", sentence);
            }

            var checksumIndex = sentence.IndexOf('*');
            if (checksumIndex == -1)
            {
                throw new AisParserException("Invalid sentence: unable to find checksum", sentence);
            }

            var checksum = ExtractChecksum(sentence, checksumIndex);

            var sentenceWithoutChecksum = sentence.Substring(0, checksumIndex);
            var calculatedChecksum = CalculateChecksum(sentenceWithoutChecksum);

            if (checksum != calculatedChecksum)
            {
                throw new AisParserException($"Invalid sentence: checksum failure. Checksum: {checksum}, calculated: {calculatedChecksum}", sentence);
            }

            var sentenceParts = sentenceWithoutChecksum.Split(',');
            var packetHeader = sentenceParts[0];
            if (!ValidPacketHeader(packetHeader))
            {
                throw new AisParserException($"Unrecognised message: packet header {packetHeader}", sentence);
            }

            // var radioChannelCode = sentenceParts[4];
            var encodedPayload = sentenceParts[5];

            if (string.IsNullOrWhiteSpace(encodedPayload))
            {
                return null;
            }

            var payload = DecodePayload(encodedPayload, Convert.ToInt32(sentenceParts[1]), Convert.ToInt32(sentenceParts[2]),
                Convert.ToInt32(sentenceParts[3]), Convert.ToInt32(sentenceParts[6]));
            return payload == null ? null : _messageFactory.Create(payload, ThrowOnUnknownMessage);
        }

        public AisMessage? Parse(NmeaSentence sentence)
        {
            // Until here, AIS messages are only known as raw sentences
            if (sentence is RawSentence rs && rs.Valid)
            {
                if (IsValidAisSentence(rs))
                {
                    string encodedPayload = rs.Fields[4];

                    if (string.IsNullOrWhiteSpace(encodedPayload))
                    {
                        return null;
                    }

                    int messageId = 0;
                    if (Int32.TryParse(rs.Fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int numFragments)
                        && Int32.TryParse(rs.Fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fragmentNumber)
                        // This field may legaly be empty (and in fact very often is)
                        && (Int32.TryParse(rs.Fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out messageId) || string.IsNullOrWhiteSpace(rs.Fields[2]))
                        && Int32.TryParse(rs.Fields[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int numFillBits))
                    {
                        var payload = DecodePayload(encodedPayload, numFragments, fragmentNumber, messageId, numFillBits);

                        return payload == null ? null : _messageFactory.Create(payload, rs.Fields[3], ThrowOnUnknownMessage);
                    }
                }
            }

            return null;
        }

        private bool IsValidAisSentence(RawSentence rs)
        {
            return rs.Valid && rs.Fields.Length == 6 && (rs.SentenceId == VdoId || rs.SentenceId == VdmId) &&
                   rs.TalkerId == TalkerId.Ais;
        }

        public string Parse<T>(T aisMessage)
            where T : AisMessage
        {
            string sentence = string.Empty;

            // Example: !AIVDM,1,1,,A,B6CdCm0t3`tba35f@V9faHi7kP06,0*58
            // Field 1: Sentence Type
            // Field 2: Count Of Fragments
            // Field 3: Fragment Number
            // Field 4: Sequential Messages ID for multi-sentence messages (blank for none)
            // Field 5: Radio Channel Code (A or B)
            // Field 6: Payload
            // Field 7: 6 bit Boundary Padding (Zero seems to always be OK)?
            string sentenceType = "AIVDM";
            int countOfFragments = 1;
            int fragmentNumber = 1;
            string radioChannel = "A";
            int boundaryPadding = 0;

            Payload payload = _messageFactory.Encode<T>(aisMessage);
            var payloadEncoded = _payloadEncoder.EncodeSixBitAis(payload);

            // Build the full sentence
            sentence += "!";
            sentence += sentenceType;
            sentence += ",";
            sentence += countOfFragments.ToString("0");
            sentence += ",";
            sentence += fragmentNumber.ToString("0");
            sentence += ",";

            sentence += ",";
            sentence += radioChannel;
            sentence += ",";
            sentence += payloadEncoded;
            sentence += ",";
            sentence += boundaryPadding.ToString("0");

            var calculatedChecksum = CalculateChecksum(sentence);
            sentence += "*" + calculatedChecksum.ToString("00");

            return sentence;
        }

        private Payload? DecodePayload(string encodedPayload, int numFragments, int fragmentNumber, int messageId, int numFillBits)
        {
            if (numFragments == 1)
            {
                var decoded = _payloadDecoder.Decode(encodedPayload, numFillBits);
                return decoded;
            }

            lock (_fragments)
            {
                if (fragmentNumber == 1)
                {
                    // Note this clears any previous message parts, which is intended (apparently the previous group with this messageId was never completed)
                    var l = new List<string>(numFragments) { encodedPayload };
                    _fragments[messageId] = l;
                    return null;
                }

                if (fragmentNumber <= numFragments)
                {
                    if (_fragments.TryGetValue(messageId, out var existingParts) && existingParts.Count == fragmentNumber - 1)
                    {
                        existingParts.Add(encodedPayload);
                    }
                    else
                    {
                        // Message is incomplete or out of order -> drop it
                        _fragments.Remove(messageId);
                        return null;
                    }
                }

                if (fragmentNumber == numFragments)
                {
                    if (_fragments.TryGetValue(messageId, out var existingParts) &&
                        existingParts.Count == numFragments)
                    {
                        // The collection is complete.
                        encodedPayload = string.Join(string.Empty, existingParts);
                        _fragments.Remove(messageId);
                        return _payloadDecoder.Decode(encodedPayload, numFillBits);
                    }
                }

                return null; // More parts expected
            }
        }

        public int ExtractChecksum(string sentence, int checksumIndex)
        {
            var checksum = sentence.Substring(checksumIndex + 1);
            return Convert.ToInt32(checksum, 16);
        }

        public int CalculateChecksum(string sentence)
        {
            var data = sentence.Substring(1);

            var checksum = 0;
            foreach (var ch in data)
            {
                checksum ^= (byte)ch;
            }

            return Convert.ToInt32(checksum.ToString("X"), 16);
        }

        private bool ValidPacketHeader(string packetHeader)
        {
            return packetHeader == "!AIVDM" || packetHeader == "!AIVDO";
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Nmea0183.Sentences;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// A parser that decodes YDWG NMEA2000 messages into PCDIN sequences.
    /// This message is used by Yacht Device's YDWG-02 Wifi-to-NMEA2000 interface.
    /// </summary>
    public class Nmea2000YdwgParser : NmeaParser
    {
        private const int TransmitConfirmationTimeout = 1000;
        private uint _currentPgn = 0;
        private List<byte> _allData = new List<byte>();
        private ulong _pgnAwaitingSend = 0;
        private Dictionary<uint, string> _unknownPgnsSeen = new Dictionary<uint, string>();

        /// <summary>
        /// Constructs an instance of this type
        /// </summary>
        /// <param name="interfaceName">Friendly name of this interface (used for filtering and eventually logging)</param>
        /// <param name="dataSource">Data source (may be connected to a serial port, a network interface, or whatever). It is recommended to use a blocking Stream,
        /// to prevent unnecessary polling</param>
        /// <param name="dataSink">Optional data sink, to send information. Can be null, and can be identical to the source stream</param>
        public Nmea2000YdwgParser(string interfaceName, Stream dataSource, Stream? dataSink)
            : base(interfaceName, dataSource, dataSink, new Raw8BitEncoding())
        {
            SenderId = 0;
        }

        /// <summary>
        /// Our own sender ID. Typically ignored during sending, as the interface substitutes its own address automatically.
        /// When using a low-level CAN-Bus interface, this needs to be set after sending an ISO Address Request Message.
        /// </summary>
        public byte SenderId
        {
            get;
            set;
        }

        /// <summary>
        /// Parses a NMEA 2000 Message in Yacht Devices RAW format.
        /// Data format is as follows:
        /// <example>
        /// 17:33:21.107 R 19F51323 01 2F 30 70 00 2F 30 70
        /// 17:33:21.108 R 19F51323 02 00
        /// 17:33:21.141 R 09F80115 A0 7D E6 18 C0 05 FB D5
        /// </example>
        /// </summary>
        /// <param name="currentLine">The current line, see example</param>
        /// <param name="error">Receives a parser error type, if any</param>
        /// <returns>A sentence in NMEA0183 raw format, or null</returns>
        protected internal override TalkerSentence? ParseSentence(string currentLine, out NmeaError error)
        {
            string[] splits = currentLine.Split(' ', StringSplitOptions.TrimEntries);
            if (splits.Length <= 4)
            {
                error = NmeaError.MessageToShort;
                return null;
            }

            if (splits[1] == "T")
            {
                if (UInt32.TryParse(splits[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint pgnTransmit))
                {
                    pgnTransmit >>= 8;
                    if (pgnTransmit == _pgnAwaitingSend)
                    {
                        Logger.LogInformation($"Received confirmation that we sent {currentLine.Trim()}");
                        Interlocked.Exchange(ref _pgnAwaitingSend, 0);
                    }
                }

                error = NmeaError.None;
                return null;
            }

            if (splits[1] != "R")
            {
                error = NmeaError.NoSyncByte;
                return null;
            }

            if (UInt32.TryParse(splits[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint pgn))
            {
                if (pgn != _currentPgn)
                {
                    _allData.Clear();
                    _currentPgn = pgn;
                }

                // This value appears to be pretty random and just indicates time since application/converter start
                TimeSpan timeStamp;
                if (!TimeSpan.TryParse(splits[0], CultureInfo.InvariantCulture, out timeStamp))
                {
                    timeStamp = TimeSpan.Zero;
                }

                _currentPgn = pgn;

                var s = string.Join(string.Empty, splits.Skip(3));
                var bytes = Convert.FromHexString(s);
                _allData.AddRange(bytes);
                var declaration = Nmea2000Declarations.GetByPgn(pgn >> 8);

                if (declaration != null)
                {
                    TalkerSentence? result = CreateSentence(declaration, timeStamp, pgn >> 8, pgn & 0xFF, _allData);
                    if (result != null)
                    {
                        error = NmeaError.None;
                        return result;
                    }
                }
                else if (declaration == null)
                {
                    uint rawpgn = (pgn >> 8) & 0x1FFFF;
                    if (_unknownPgnsSeen.TryAdd(rawpgn, currentLine))
                    {
                        Logger.LogInformation($"New Unknown PGN: {rawpgn:X6}");
                    }

                    error = NmeaError.None;
                    return null;
                }

                // Message is known, but incomplete
                error = NmeaError.None;
                return null;
            }

            error = NmeaError.NoSyncByte;
            return null;
        }

        /// <summary>
        /// Returns the list of (so-far) seen unknown PGNs together with an example payload.
        /// </summary>
        /// <returns>A dictionary</returns>
        public Dictionary<uint, string> GetListOfUnknownPgns()
        {
            return _unknownPgnsSeen;
        }

        private TalkerSentence? CreateSentence(Nmea2000PgnDeclaration declaration, TimeSpan? timeStamp, uint pgn, uint sender, List<byte> allData)
        {
            if (allData.Count < declaration.Length && declaration.FastPacket == false)
            {
                return null;
            }

            var fields = new List<string>();

            // Should usually be equivalent to declaration.Pgn, but may include a destination address,
            // which we shouldn't be loosing
            fields.Add($"{pgn:X6}");
            if (timeStamp.HasValue)
            {
                fields.Add($"{((long)timeStamp.Value.TotalSeconds):X8}");
            }
            else
            {
                fields.Add("00000000");
            }

            fields.Add($"{sender:X2}");
            if (declaration.FastPacket)
            {
                if (allData.Count < 2)
                {
                    Logger.LogWarning("Found a FastPacket message with less than 2 bytes");
                    return null;
                }

                int sequenceIdentifier = allData[0] >> 5;
                int sequenceNo = allData[0] & 0x1F;
                if (sequenceNo != 0)
                {
                    allData.Clear();
                    return null;
                }

                int dataLength = allData[1];
                List<byte> fullSequence = new List<byte>(dataLength);
                int srcIndex = 2;
                while (srcIndex < allData.Count)
                {
                    fullSequence.Add(allData[srcIndex]);

                    srcIndex++;
                    if (srcIndex >= allData.Count)
                    {
                        break;
                    }

                    if (srcIndex % 8 == 0)
                    {
                        int newSequenceIdentifier = allData[srcIndex] >> 5;
                        if (newSequenceIdentifier != sequenceIdentifier)
                        {
                            allData.Clear();
                            return null;
                        }

                        int newSequenceNo = allData[srcIndex] & 0x1F;
                        if (newSequenceNo != sequenceNo + 1)
                        {
                            allData.Clear();
                            return null;
                        }

                        sequenceNo++;
                        // If this indeed is the continuation of the correct message, skip the byte and continue
                        srcIndex++;
                    }
                }

                if (fullSequence.Count >= dataLength)
                {
                    fields.Add(Convert.ToHexString(fullSequence.ToArray()));
                }
                else
                {
                    return null;
                }
            }
            else
            {
                fields.Add(Convert.ToHexString(allData.ToArray()));
            }

            var ret = new TalkerSentence(TalkerId.Proprietary, Nmea2000PackedMessage.Id, fields);
            return ret;
        }

        /// <inheritdoc/>
        protected internal override void FormatAndSendSentence(NmeaSentence sentence)
        {
            if (sentence is Nmea2000PackedMessage)
            {
                // This is a bit hacky, as we go through the string representation of the object.
                // But having also a binary representation increases complexity for the individual messages.
                // Maybe we improve that later.
                string nmea0183 = sentence.ToNmeaParameterList();
                string[] splits = nmea0183.Split(',', StringSplitOptions.TrimEntries);
                if (!uint.TryParse(splits[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint pgn))
                {
                    Logger.LogError($"Attempting to send invalid composed message: {nmea0183}");
                }

                StringBuilder data = new StringBuilder(splits[3]);

                int idx = 0;
                while (idx < data.Length)
                {
                    idx += 2;
                    data.Insert(idx, ' ');
                    idx += 1;
                }

                int loops = TransmitConfirmationTimeout / 20;
                while (Interlocked.Read(ref _pgnAwaitingSend) != 0 && loops-- >= 0)
                {
                    Thread.Sleep(10);
                }

                if (loops < 0)
                {
                    Logger.LogWarning($"Previous outgoing message with pgn {pgn:X8} was not confirmed");
                }

                Interlocked.Exchange(ref _pgnAwaitingSend, pgn);

                // Wait until event is set
                // This does not yet support fast packet data
                string sendData = $"{pgn << 8:X8} {data}\r\n";
                byte[] buffer = StreamEncoding.GetBytes(sendData);

                Sink?.Write(buffer, 0, buffer.Length);
            }
            else
            {
                Logger.LogWarning("Can only send Nmea2000PackedMessage instances with this interface ($PCDIN sequences)");
                return;
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            foreach (var kp in _unknownPgnsSeen)
            {
                Logger.LogInformation($"PGN {kp.Key} is not known but was seen");
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Nmea0183.Sentences;
using Microsoft.Extensions.Logging;

namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// A parser that decodes YDWG NMEA2000 messages into PCDIN sequences.
    /// This message is used by Yacht Device's YDWG-02 Wifi-to-NMEA2000 interface.
    /// </summary>
    public class Nmea2000YdwgParser : NmeaParser
    {
        private uint _currentPgn = 0;
        private List<byte> _allData = new List<byte>();

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
        /// Our own sender ID. Can be left at 0 usually, which will cause the interface to substitute
        /// the correct sender ID when transmitting the package
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
                // TODO: Maybe wait for this somewhere?
                Logger.LogInformation($"Received confirmation that we sent {currentLine}");
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
                var declaration = Nmea2000Declarations.GetByPgn(pgn);
                if (declaration != null && declaration.IsComplete(_allData))
                {
                    var result = CreateSentence(declaration, timeStamp, pgn & 0xFF, _allData);
                    if (result != null)
                    {
                        error = NmeaError.None;
                        return result;
                    }
                }
                else if (declaration == null)
                {
                    uint rawpgn = (pgn >> 8) & 0x1FFFF;
                    Logger.LogInformation($"Unknown PGN: {rawpgn:X6}");
                    error = NmeaError.None;
                    return null;
                }
                else
                {
                    // Message is known, but incomplete
                    error = NmeaError.None;
                    return null;
                }
            }

            error = NmeaError.NoSyncByte;
            return null;
        }

        private TalkerSentence CreateSentence(Nmea2000PgnDeclaration declaration, TimeSpan? timeStamp, uint sender, List<byte> allData)
        {
            if (declaration.IsComplete(allData) == false)
            {
                throw new InvalidOperationException("Invalid sequencing: Message was already complete now it's not?");
            }

            var fields = new List<string>();

            fields.Add($"{declaration.Pgn:X6}");
            if (timeStamp.HasValue)
            {
                fields.Add($"{((long)timeStamp.Value.TotalSeconds):X8}");
            }
            else
            {
                fields.Add("00000000");
            }

            fields.Add($"{sender:X2}");
            fields.Add(Convert.ToHexString(allData.ToArray()));

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
    }
}

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

namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// A parser that decodes RAW NMEA2000 messages into PCDIN sequences (so we can handle them as if
    /// they were NMEA0183 messages)
    /// </summary>
    public class Nmea2000AsciiParser : NmeaParser
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
        public Nmea2000AsciiParser(string interfaceName, Stream dataSource, Stream? dataSink)
            : base(interfaceName, dataSource, dataSink)
        {
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
        protected override TalkerSentence? ParseSentence(string currentLine, out NmeaError error)
        {
            string[] splits = currentLine.Split(' ', StringSplitOptions.TrimEntries);
            if (splits.Length <= 4)
            {
                error = NmeaError.MessageToShort;
                return null;
            }

            if (splits[1] != "R")
            {
                error = NmeaError.NoSyncByte;
                return null;
            }

            if (UInt32.TryParse(splits[2], CultureInfo.InvariantCulture, out uint pgn))
            {
                if (pgn != _currentPgn)
                {
                    _allData.Clear();
                    _currentPgn = pgn;
                }

                var bytes = Convert.FromHexString(string.Join('-', splits.Skip(3)));
                _allData.AddRange(bytes);
                var declaration = Nmea2000Declarations.GetByPgn(pgn);
                if (declaration != null && declaration.IsComplete(_allData))
                {
                    var result = CreateSentence(declaration, _allData);
                    if (result != null)
                    {
                        error = NmeaError.None;
                        return result;
                    }
                }
            }

            error = NmeaError.NoSyncByte;
            return null;
        }

        private TalkerSentence? CreateSentence(Nmea2000PgnDeclaration declaration, List<byte> allData)
        {
            if (declaration.IsComplete(allData) == false)
            {
                throw new InvalidOperationException("Invalid sequencing: Message was already complete now it's not?");
            }

            var fields = new List<string>();

            fields.Add($"{declaration.Pgn:X6}");

            var ret = new TalkerSentence(TalkerId.Proprietary, Nmea2000PackedMessage.Id, fields);
            return ret;
        }
    }
}

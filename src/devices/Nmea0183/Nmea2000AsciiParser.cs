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

        public Nmea2000AsciiParser(string interfaceName, Stream dataSource, Stream? dataSink)
            : base(interfaceName, dataSource, dataSink)
        {
        }

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

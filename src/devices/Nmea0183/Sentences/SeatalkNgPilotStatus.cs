// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183.Sentences
{
    public class SeatalkNgPilotStatus : Nmea2000PackedMessage
    {
        /// <summary>
        /// Hexadecimal identifier for this message
        /// </summary>
        public const int HexId = 0x0FF63;

        private uint _manufacturerAndIndustry;

        public AutopilotStatus PilotStatus
        {
            get;
            set;
        }

        public override bool ReplacesOlderInstance => true;

        public override int Identifier => HexId;

        public override string ToReadableContent()
        {
            return $"SeatalkNg Pilot status: {PilotStatus}";
        }

        public SeatalkNgPilotStatus(AutopilotStatus status)
        {
            _manufacturerAndIndustry = ManufacturerRaymarine;
            PilotStatus = status;
            Valid = true;
        }

        /// <summary>
        /// Create a message object from a sentence
        /// </summary>
        /// <param name="sentence">The sentence</param>
        /// <param name="time">The current time</param>
        public SeatalkNgPilotStatus(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Creates a message object from a decoded sentence
        /// </summary>
        /// <param name="talkerId">The source talker id</param>
        /// <param name="fields">The parameters</param>
        /// <param name="time">The current time</param>
        public SeatalkNgPilotStatus(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            ParseCommonFields(field);

            string data = ReadString(field);

            if (ReadFromHexString(data, 0, 4, false, out int manf))
            {
                _manufacturerAndIndustry = (uint)manf;
            }

            if (ReadFromHexString(data, 4, 4, false, out int status))
            {
                PilotStatus = (AutopilotStatus)status; // TODO: Fix this
            }

            Valid = true;
        }

        public override string ToNmeaParameterList()
        {
            string manufacturer = _manufacturerAndIndustry.ToString("X4", CultureInfo.InvariantCulture);
            // Ugly hack to see which messages are required
            if (PilotStatus == AutopilotStatus.Standby)
            {
                string pgn = Identifier.ToString("X6", CultureInfo.InvariantCulture);
                string timeStampText = MessageTimeStamp.ToString("X8", CultureInfo.InvariantCulture);
                string source = MessageSource.ToString("X2", CultureInfo.InvariantCulture);
                string hd = $"{pgn},{timeStampText},{source},";
                return hd + manufacturer + "0000000007FF";
            }
            else if (PilotStatus == AutopilotStatus.Auto)
            {
                string pgn = 65359.ToString("X6", CultureInfo.InvariantCulture);
                string timeStampText = MessageTimeStamp.ToString("X8", CultureInfo.InvariantCulture);
                string source = MessageSource.ToString("X2", CultureInfo.InvariantCulture);
                string hd = $"{pgn},{timeStampText},{source},";
                return hd + manufacturer + "FFFFFF44EDFF";
            }
            else
            {
                string pgn = 65360.ToString("X6", CultureInfo.InvariantCulture);
                string timeStampText = MessageTimeStamp.ToString("X8", CultureInfo.InvariantCulture);
                string source = MessageSource.ToString("X2", CultureInfo.InvariantCulture);
                string hd = $"{pgn},{timeStampText},{source},";
                return hd + manufacturer + "FFFFFF30EEFF";
            }
        }
    }
}

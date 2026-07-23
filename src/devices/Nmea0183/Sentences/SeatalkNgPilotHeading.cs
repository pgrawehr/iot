// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Common;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183.Sentences
{
    public class SeatalkNgPilotHeading : Nmea2000PackedMessage
    {
        /// <summary>
        /// Hexadecimal identifier for this message
        /// </summary>
        public const int HexId = 0x0FF4F;

        private uint _manufacturerAndIndustry;

        public Angle? HeadingTrue
        {
            get;
            set;
        }

        public Angle? HeadingMagnetic
        {
            get;
            set;
        }

        /// <summary>
        /// What is this?
        /// </summary>
        public int Sid
        {
            get;
            set;
        }

        public override uint Identifier => HexId;

        public override string ToReadableContent()
        {
            return $"SeatalkNg Pilot Heading status: {HeadingMagnetic}° Mag, {HeadingTrue}° True";
        }

        public SeatalkNgPilotHeading(Angle? headingTrue, Angle? headingMagnetic)
        {
            _manufacturerAndIndustry = ManufacturerRaymarine;
            HeadingTrue = headingTrue;
            HeadingMagnetic = headingMagnetic;
            Sid = 0xFF;
            Valid = true;
        }

        /// <summary>
        /// Create a message object from a sentence
        /// </summary>
        /// <param name="sentence">The sentence</param>
        /// <param name="time">The current time</param>
        public SeatalkNgPilotHeading(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Creates a message object from a decoded sentence
        /// </summary>
        /// <param name="talkerId">The source talker id</param>
        /// <param name="fields">The parameters</param>
        /// <param name="time">The current time</param>
        public SeatalkNgPilotHeading(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            ParseCommonFields(field);

            string data = ReadString(field);

            if (ReadFromHexString(data, 0, 4, false, out int manf))
            {
                _manufacturerAndIndustry = (uint)manf;
            }

            if (ReadFromHexString(data, 4, 2, false, out int status))
            {
                Sid = status;
            }

            HeadingTrue = null;
            HeadingMagnetic = null;

            int v = 0;
            if (ReadFromHexString(data, 6, 4, true, out v))
            {
                if (v != 0xFFFF)
                {
                    HeadingTrue = Angle.FromRadians((double)v / 0.0001);
                }
            }

            if (ReadFromHexString(data, 10, 4, true, out v))
            {
                if (v != 0xFFFF)
                {
                    HeadingMagnetic = Angle.FromRadians((double)v * 0.0001);
                }
            }

            Valid = true;
        }

        /// <inheritdoc/>
        public override bool ReplacesOlderInstance => true;

        public override string ToNmeaParameterList()
        {
            string manufacturer = _manufacturerAndIndustry.ToString("X4", CultureInfo.InvariantCulture);

            string trueAngle = DoubleTo16BitField(HeadingTrue.HasValue ? HeadingTrue.Value.Normalize(true).Radians : null,
                0.0001).ToString("X4", CultureInfo.InvariantCulture);
            trueAngle = trueAngle.Substring(2, 2) + trueAngle.Substring(0, 2);

            string magAngle = DoubleTo16BitField(HeadingMagnetic.HasValue ? HeadingMagnetic.Value.Normalize(true).Radians : null,
                0.0001).ToString("X4", CultureInfo.InvariantCulture);
            magAngle = magAngle.Substring(2, 2) + magAngle.Substring(0, 2);
            return base.ToNmeaParameterList() + manufacturer + Sid.ToString("X2", CultureInfo.InvariantCulture) + trueAngle + magAngle + "FF";
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

#pragma warning disable CS1591

namespace Iot.Device.Nmea0183.Sentences
{
    public class Rudder : Nmea2000PackedMessage
    {
        public const int HexId = 0x1F10D;
        public override bool ReplacesOlderInstance => true;

        public Angle? ActualAngle
        {
            get;
            set;
        }

        public Angle? DesiredAngle
        {
            get;
            set;
        }

        public byte Instance
        {
            get;
            set;
        }

        public byte DirectionOrder
        {
            get;
            set;
        }

        public Rudder(Angle? actualAngle, Angle? desiredAngle, byte directionOrder, byte instance)
        {
            Instance = instance;
            ActualAngle = actualAngle;
            DesiredAngle = desiredAngle;
            DirectionOrder = directionOrder;
            Valid = true;
        }

        /// <summary>
        /// Create a message object from a sentence
        /// </summary>
        /// <param name="sentence">The sentence</param>
        /// <param name="time">The current time</param>
        public Rudder(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Creates a message object from a decoded sentence
        /// </summary>
        /// <param name="talkerId">The source talker id</param>
        /// <param name="fields">The parameters</param>
        /// <param name="time">The current time</param>
        public Rudder(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            ParseCommonFields(field);

            string data = ReadString(field);

            if (ReadFromHexString(data, 0, 2, false, out int v))
            {
                Instance = (byte)v;
            }

            if (ReadFromHexString(data, 2, 2, false, out v))
            {
                DirectionOrder = (byte)(v >> 5);
            }

            if (ReadFromHexString(data, 4, 4, true, out v) && v != 0x7FFF)
            {
                DesiredAngle = Angle.FromRadians(0.0001 * v);
            }

            if (ReadFromHexString(data, 8, 4, true, out v) && v != 0x7FFF)
            {
                ActualAngle = Angle.FromRadians(0.0001 * v);
            }

            Valid = true;
        }

        public override string ToNmeaParameterList()
        {
            throw new NotImplementedException();
        }

        public override string ToReadableContent()
        {
            return $"Rudder Angle: {ActualAngle}, Desired Angle {DesiredAngle}";
        }

        public override uint Identifier => HexId;
    }
}

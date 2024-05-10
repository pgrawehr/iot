// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Seatalk1.Messages
{
    /// <summary>
    /// This message reports the last 4 characters of the next waypoint
    /// </summary>
    public record TargetWaypointName : SeatalkMessage
    {
        internal TargetWaypointName()
        {
            Name = string.Empty;
        }

        /// <summary>
        /// Creates a new instance of this class
        /// </summary>
        /// <param name="name">The name of the waypoint</param>
        public TargetWaypointName(string name)
        {
            Name = name;
        }

        /// <inheritdoc />
        public override byte CommandByte => 0x82;

        /// <inheritdoc />
        public override byte ExpectedLength => 8;

        /// <summary>
        /// The name of the waypoint. Max 4 characters (the rest is ignored)
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <inheritdoc />
        public override SeatalkMessage CreateNewMessage(IReadOnlyList<byte> data)
        {
            // 82  05  XX  xx YY yy ZZ zz   Target waypoint name, with XX+yy == 0xff etc.
            int c1, c2, c3, c4;
            c1 = data[2] & 0x3F;
            c2 = ((data[4] & 0xF) * 4) + ((data[2] & 0xC0) / 64);
            c3 = ((data[6] & 0x3) * 16) + ((data[4] & 0xF0) / 16);
            c4 = (data[6] & 0xFC) / 4;
            c1 += 0x30;
            c2 += 0x30;
            c3 += 0x30;
            c4 += 0x30;
            string name = $"{(char)c1}{(char)c2}{(char)c3}{(char)c4}";
            return new TargetWaypointName(name);
        }

        /// <inheritdoc />
        public override byte[] CreateDatagram()
        {
            // Not implemented correctly yet
            return new byte[]
            {
                CommandByte, (byte)(ExpectedLength - 3), 0x0, 0xff, 0x0, 0xff, 0x0, 0xff
            };
        }

        /// <inheritdoc />
        public override bool MatchesMessageType(IReadOnlyList<byte> data)
        {
            return base.MatchesMessageType(data) && (data[2] + data[3]) == 0xFF;
        }
    }
}

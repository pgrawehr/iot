// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// This class holds static information about a particular NMEA2000 PGN (message type)
    /// </summary>
    /// <param name="Pgn">The message number (usually given in hex)</param>
    /// <param name="Name">The name of the message</param>
    /// <param name="Priority">The typical priority this message uses</param>
    /// <param name="Length">The length of the data part of this message, in bytes. Negative to indicate "at least x bytes"</param>
    /// <param name="FastPacket">True if this PGN typically consists of more than one packet</param>
    public sealed record class Nmea2000PgnDeclaration(uint Pgn, string Name, int Priority, int Length, bool FastPacket)
    {
    }
}

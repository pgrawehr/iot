using System;
using System.Collections.Generic;
using System.Text;

namespace Nmea0183.Sentences
{
    internal enum CardinalDirection : byte
    {
        None = 0,
        North = (byte)'N',
        South = (byte)'S',
        West = (byte)'W',
        East = (byte)'E',
    }
}

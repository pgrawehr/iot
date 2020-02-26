using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable 1591
namespace Nmea0183
{
    public enum NmeaError
    {
        None = 0,
        MessageToShort,
        InvalidChecksum,
        MessageToLong,
        NoSyncByte
    }
}

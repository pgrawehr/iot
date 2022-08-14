// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Iot.Device.Common;

namespace Iot.Device.Nmea0183.Ais
{
    public class BaseStation
    {
        public BaseStation(uint mmsi)
        {
            Mmsi = mmsi;
            Position = new GeographicPosition();
        }

        public uint Mmsi { get; }
        public DateTimeOffset LastSeen { get; set; }
        public AisTransceiverClass TransceiverClass { get; set; }
        public GeographicPosition Position { get; set; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Iot.Device.Common;

namespace Iot.Device.Nmea0183.Ais
{
    public class AidToNavigation
    {
        public AidToNavigation(uint mmsi)
        {
            Mmsi = mmsi;
            Name = string.Empty;
            Position = new GeographicPosition();
        }

        public uint Mmsi { get; }

        public string Name { get; set; }

        public GeographicPosition Position { get; set; }
    }
}

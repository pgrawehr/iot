// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Common;

namespace Iot.Device.Nmea0183.Ais
{
    public class Ship
    {
        public Ship(uint mmsi)
        {
            Mmsi = mmsi;
            LastSeen = DateTimeOffset.UtcNow;
            Position = new GeographicPosition();
        }

        public uint Mmsi
        {
            get;
        }

        public DateTimeOffset LastSeen
        {
            get;
            set;
        }

        public string? Name
        {
            get;
            set;
        }

        public GeographicPosition Position
        {
            get;
            set;
        }
    }
}

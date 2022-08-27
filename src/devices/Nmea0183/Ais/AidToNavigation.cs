// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Iot.Device.Common;

namespace Iot.Device.Nmea0183.Ais
{
    public class AidToNavigation : AisTarget
    {
        public AidToNavigation(uint mmsi)
        : base(mmsi)
        {
            Position = new GeographicPosition();
        }
    }
}

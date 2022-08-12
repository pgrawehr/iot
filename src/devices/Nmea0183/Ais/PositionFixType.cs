// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Iot.Device.Nmea0183.Ais
{
    public enum PositionFixType
    {
        Undefined1,
        Gps,
        Glonass,
        CombinedGpsAndGlonass,
        LoranC,
        Chayka,
        IntegratedNavigationSystem,
        Surveyed,
        Galileo,
        Undefined2 = 15
    }
}
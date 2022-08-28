// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Iot.Device.Nmea0183.Ais
{
    /// <summary>
    /// Type of navigational aid for AtoN targets.
    /// </summary>
    public enum NavigationalAidType
    {
        NotSpecified,
        ReferencePoint,
        Racon,
        FixedStuctureOffShore,
        Spare,
        LightWithoutSectors,
        LightWithSectors,
        LeadingLightFront,
        LeadingLigthRear,
        BeaconCardinalN,
        BeaconCardinalE,
        BeaconCardinalS,
        BeaconCardinalW,
        BeaconPortHand,
        BeaconStarboardHand,
        BeaconPreferredChannelPortHand,
        BeaconPreferredChannelStarboardHand,
        BeaconIsolatedDanger,
        BeaconSafeWater,
        BeaconSpecialMark,
        CardinalMarkN,
        CardinalMarkE,
        CardinalMarkS,
        CardinalMarkW,
        PortHandMark,
        StarboardHandMark,
        PreferredChannelPortHand,
        PreferredChannelStarboardHand,
        IsolatedDanger,
        SafeWater,
        SpecialMark,
        LightVessel
    }
}

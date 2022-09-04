// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Iot.Device.Common;
using UnitsNet;

namespace Iot.Device.Nmea0183.Ais
{
    public record AidToNavigation : AisTarget
    {
        public AidToNavigation(uint mmsi)
        : base(mmsi)
        {
            Position = new GeographicPosition();
        }

        public Length DimensionToBow { get; set; }

        public Length DimensionToStern { get; set; }

        public Length DimensionToPort { get; set; }

        public Length DimensionToStarboard { get; set; }

        public Length Length => DimensionToBow + DimensionToStern;

        public Length Beam => DimensionToPort + DimensionToStarboard;

        /// <summary>
        /// True if the beacon is off position. Can only happen for real AtoN targets.
        /// If this is true, caution is advised, because the beacon is floating in a wrong
        /// position, endangering ships, and it's missing where it should be.
        /// </summary>
        public bool OffPosition { get; set; }

        /// <summary>
        /// True if this is a virtual aid-to-navigation target. There's typically no
        /// visible buoy at the given position, and the signal is sent from a remote base station.
        /// </summary>
        public bool Virtual { get; set; }

        /// <summary>
        /// The type of navigational aid this target indicates.
        /// </summary>
        public NavigationalAidType NavigationalAidType { get; set; }
    }
}

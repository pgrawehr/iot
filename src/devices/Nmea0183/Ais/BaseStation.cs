// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Iot.Device.Common;

namespace Iot.Device.Nmea0183.Ais
{
    public record BaseStation : AisTarget
    {
        /// <summary>
        ///  Base stations have no name in their data (just the country identifier)
        /// </summary>
        private const string BaseStationName = "Base Station";

        public BaseStation(uint mmsi)
        : base(mmsi)
        {
            Position = new GeographicPosition();
            Name = BaseStationName;
        }

        public AisTransceiverClass TransceiverClass { get; set; }

        public override string ToString()
        {
            string s = FormatMmsi() + $"({Name})";

            if (Position.ContainsValidPosition())
            {
                s += $" {Position}";
            }

            return s;
        }
    }
}

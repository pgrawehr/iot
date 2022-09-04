// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Iot.Device.Common;
using UnitsNet;

namespace Iot.Device.Nmea0183.Ais
{
    public record Ship : AisTarget
    {
        public Ship(uint mmsi)
        : base(mmsi)
        {
            LastSeen = DateTimeOffset.UtcNow;
            CallSign = string.Empty;
            Destination = string.Empty;
        }

        public RotationalSpeed? RateOfTurn { get; set; }
        public Angle? TrueHeading { get; set; }
        public Angle CourseOverGround { get; set; }
        public Speed SpeedOverGround { get; set; }
        public string CallSign { get; set; }
        public ShipType ShipType { get; set; }
        public Length DimensionToBow { get; set; }
        public Length DimensionToStern { get; set; }
        public Length DimensionToPort { get; set; }
        public Length DimensionToStarboard { get; set; }

        /// <summary>
        /// The transceiver type this target uses.
        /// </summary>
        public AisTransceiverClass TransceiverClass { get; set; }

        public Length Length => DimensionToBow + DimensionToStern;

        public Length Beam => DimensionToPort + DimensionToStarboard;
        public DateTimeOffset? EstimatedTimeOfArrival { get; set; }
        public string Destination { get; set; }
        public Length? Draught { get; set; }

        public uint ImoNumber { get; set; }

        public NavigationStatus NavigationStatus { get; set; }

        public override string ToString()
        {
            string s = Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(s))
            {
                s = FormatMmsi();
            }

            if (TransceiverClass == AisTransceiverClass.A)
            {
                s += " (Class A)";
            }
            else if (TransceiverClass == AisTransceiverClass.B)
            {
                s += " (Class B)";
            }

            if (Position.ContainsValidPosition())
            {
                s += $" {Position}";
            }

            return s;
        }
    }
}

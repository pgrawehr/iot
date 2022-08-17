// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
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
            Name = string.Empty;
            CallSign = string.Empty;
            Destination = string.Empty;
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

        public string Name
        {
            get;
            set;
        }

        public GeographicPosition Position
        {
            get;
            set;
        }

        public int? RateOfTurn { get; set; }
        public uint? TrueHeading { get; set; }
        public double CourseOverGround { get; set; }
        public double SpeedOverGround { get; set; }
        public string CallSign { get; set; }
        public ShipType ShipType { get; set; }
        public uint DimensionToBow { get; set; }
        public uint DimensionToStern { get; set; }
        public uint DimensionToPort { get; set; }
        public uint DimensionToStarboard { get; set; }

        public AisTransceiverClass TransceiverClass { get; set; }

        public uint Length => DimensionToBow + DimensionToStern;

        public uint Beam => DimensionToPort + DimensionToStarboard;
        public DateTimeOffset? EstimatedTimeOfArrival { get; set; }
        public string Destination { get; set; }
        public double? Draught { get; set; }

        public override string ToString()
        {
            string s = Name;
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

        /// <summary>
        /// Returns the MMSI in user-readable format (always 9 digits)
        /// </summary>
        /// <returns>The MMSI as string</returns>
        public string FormatMmsi()
        {
            string m = Mmsi.ToString(CultureInfo.InvariantCulture);
            if (m.Length == 7)
            {
                m = "00" + m; // base station id
            }
            else if (m.Length == 8)
            {
                m = "0" + m; // group id (very rare)
            }

            return m;
        }

        public MmsiType IdentifyMmsiType()
        {
            // We need to look at the first few digits. That's easiest in string format.
            string asString = FormatMmsi();

            if (asString.StartsWith("00", StringComparison.Ordinal))
            {
                return MmsiType.Group;
            }

            if (asString.StartsWith("0", StringComparison.Ordinal))
            {
                return MmsiType.Group;
            }

            if (asString.StartsWith("111", StringComparison.Ordinal))
            {
                return MmsiType.SarAircraft;
            }

            if (asString.StartsWith("99", StringComparison.Ordinal))
            {
                return MmsiType.AtoN;
            }

            if (asString.StartsWith("98", StringComparison.Ordinal))
            {
                return MmsiType.Auxiliary;
            }

            if (asString.StartsWith("970", StringComparison.Ordinal))
            {
                return MmsiType.AisSart;
            }

            if (asString.StartsWith("972", StringComparison.Ordinal))
            {
                return MmsiType.Mob;
            }

            if (asString.StartsWith("974", StringComparison.Ordinal))
            {
                return MmsiType.Epirb;
            }

            // Anything using an 1 or 9 and not handled in the cases above, is not defined.
            if (asString.StartsWith("1", StringComparison.Ordinal))
            {
                return MmsiType.Unknown;
            }

            if (asString.StartsWith("9", StringComparison.Ordinal))
            {
                return MmsiType.Unknown;
            }

            if (asString.StartsWith("8", StringComparison.Ordinal))
            {
                return MmsiType.DiversRadio;
            }

            return MmsiType.Ship;
        }
    }
}

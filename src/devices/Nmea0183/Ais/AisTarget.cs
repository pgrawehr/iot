﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Iot.Device.Common;
using Iot.Device.Nmea0183.AisSentences;

namespace Iot.Device.Nmea0183.Ais
{
    /// <summary>
    /// Generic base class for all types of AIS targets
    /// </summary>
    public abstract class AisTarget
    {
        public AisTarget(uint mmsi)
        {
            Mmsi = mmsi;
            Position = new GeographicPosition();
        }

        /// <summary>
        /// The MMSI (maritime service identification number) of this target. This is the key element in all messages to identify
        /// who sent a message.
        /// </summary>
        public uint Mmsi { get; }

        /// <summary>
        /// The time when we received the last message from this target
        /// </summary>
        public DateTimeOffset LastSeen
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the target. This is nullable, to be able to differentiate between targets whose name
        /// was not yet received (e.g. no <see cref="PositionReportClassAMessage"/> was seen) and targets that actually send an empty name.
        /// </summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// The last known position of the target
        /// </summary>
        public GeographicPosition Position
        {
            get;
            set;
        }

        public override string ToString()
        {
            string s = Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(s))
            {
                s = FormatMmsi();
            }

            // Note that the special target types (AtoN, SAR, BaseStation) do not use the notification of transceiver types.
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

            // Normally, an MMSI should start with at most two zeros, but some targets report otherwise illegal 4-digit MMSI numbers.
            while (m.Length < 9)
            {
                m = "0" + m;
            }

            return m;
        }

        public MmsiType IdentifyMmsiType()
        {
            // We need to look at the first few digits. That's easiest in string format.
            string asString = FormatMmsi();

            if (asString.StartsWith("000", StringComparison.Ordinal))
            {
                return MmsiType.Unknown; // That's not defined.
            }

            if (asString.StartsWith("00", StringComparison.Ordinal))
            {
                return MmsiType.BaseStation;
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
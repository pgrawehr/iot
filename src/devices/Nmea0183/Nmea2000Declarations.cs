// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// Static class that declares NMEA2000 message metadata
    /// </summary>
    public static class Nmea2000Declarations
    {
        private static Dictionary<uint, Nmea2000PgnDeclaration> s_data;

        static Nmea2000Declarations()
        {
            s_data = new Dictionary<uint, Nmea2000PgnDeclaration>();
            s_data.Add(0x1F010, new Nmea2000PgnDeclaration(0x1F010, "System Time", 3, 8, false));
            s_data.Add(0x1F801, new Nmea2000PgnDeclaration(0x1F801, "Position, Rapid Update", 2, 8, false));
            s_data.Add(0x1F200, new Nmea2000PgnDeclaration(0x1F200, "Engine Parameters, Rapid update", 2, 8, false));
            s_data.Add(0x1F201, new Nmea2000PgnDeclaration(0x1F201, "Engine Parameters, dynamic", 2, 26, false));
        }

        /// <summary>
        /// Gets the declaration for a particular PGN
        /// </summary>
        /// <param name="pgn">The PGN to search for</param>
        /// <returns>The data for that PGN, or null if the PGN is unknown</returns>
        public static Nmea2000PgnDeclaration? GetByPgn(uint pgn)
        {
            pgn = (pgn >> 8) & 0x1FFFF;
            if (s_data.TryGetValue(pgn, out var data))
            {
                return data;
            }

            return null;
        }
    }
}

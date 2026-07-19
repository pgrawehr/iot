// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Nmea0183.Sentences;

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
            s_data.Add(0x1F201, new Nmea2000PgnDeclaration(0x1F201, "Engine Parameters, dynamic", 2, 26, true));
            s_data.Add(SeatalkNgPilotStatus.HexId, new Nmea2000PgnDeclaration(SeatalkNgPilotStatus.HexId, "Seatalk: Pilot Mode", 7,
                8, false,
                new List<FieldDeclaration>()
                {
                    // Note: The length provided here is the number of bytes the field uses in the GroupFunction message
                    new FieldDeclaration(1, 2, "Manufacturer", 1851, x => (x >> 5) & 0x7FF),
                    new FieldDeclaration(2, 1, "Reserved", null),
                    new FieldDeclaration(3, 1, "Industry Code", 4, x => x & 0x7),
                    new FieldDeclaration(4, 2, "Pilot Mode", null),
                    new FieldDeclaration(5, 2, "Sub Mode", null)
                }));
            s_data.Add(GroupFunctionMessage.HexId, new Nmea2000PgnDeclaration(GroupFunctionMessage.HexId, "Request Group Function", 3, -1, true));
        }

        /// <summary>
        /// Gets the declaration for a particular PGN
        /// </summary>
        /// <param name="pgn">The PGN to search for</param>
        /// <returns>The data for that PGN, or null if the PGN is unknown</returns>
        public static Nmea2000PgnDeclaration? GetByPgn(uint pgn)
        {
            pgn = pgn & 0x1FFFF;
            // Ignore the lower byte of the PGN for this message (here, that's the target address)
            if ((pgn & 0x1FF00) == GroupFunctionMessage.HexId)
            {
                pgn = GroupFunctionMessage.HexId;
            }

            if (s_data.TryGetValue(pgn, out var data))
            {
                return data;
            }

            return null;
        }
    }
}

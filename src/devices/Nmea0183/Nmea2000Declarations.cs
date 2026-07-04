using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Nmea0183
{
    public static class Nmea2000Declarations
    {
        private static Dictionary<uint, Nmea2000PgnDeclaration> s_data;

        static Nmea2000Declarations()
        {
            s_data = new Dictionary<uint, Nmea2000PgnDeclaration>();
            s_data.Add(0x1F010, new Nmea2000PgnDeclaration(0x1F010, "System Time", 3, 8, false));
        }

        public static Nmea2000PgnDeclaration? GetByPgn(uint pgn)
        {
            if (s_data.TryGetValue(pgn, out var data))
            {
                return data;
            }

            return null;
        }
    }
}

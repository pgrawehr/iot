using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Nmea0183
{
    public sealed record class Nmea2000PgnDeclaration(uint Pgn, string Name, int Priority, int Length, bool FastPacket)
    {
        public bool IsComplete(List<byte> allData)
        {
            // Todo: This needs to be more complicated for messages with a dynamic length
            if (allData.Count == Length)
            {
                return true;
            }

            return false;
        }
    }
}

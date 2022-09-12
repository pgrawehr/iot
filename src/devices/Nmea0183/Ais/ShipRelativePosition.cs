using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace Iot.Device.Nmea0183.Ais
{
    /// <summary>
    /// Contains comparative position information between two ships, such as distance, bearing or estimated time to closest encounter.
    /// </summary>
    public class ShipRelativePosition
    {
        public ShipRelativePosition(AisTarget from, AisTarget to, Length distance, Angle bearing)
        {
            Distance = distance;
            Bearing = bearing;
            From = from;
            To = to;
        }

        /// <summary>
        /// Ship from which this relative position is seen (typically the own ship, but of course it's also possible to
        /// calculate possible collision vectors between arbitrary ships)
        /// </summary>
        public AisTarget From { get; }

        /// <summary>
        /// Ship to which this relative position is calculated
        /// </summary>
        public AisTarget To { get; }

        /// <summary>
        /// The current distance between the ships
        /// </summary>
        public Length Distance { get; }

        /// <summary>
        /// The bearing between the sips (Compass direction from <see cref="From"/> to <see cref="To"/>)
        /// </summary>
        public Angle Bearing { get; }

        /// <summary>
        /// Direction in which the other ship is seen from <see cref="From"/> when looking to the bow. A negative value means the other
        /// ship is on the port bow, a positive value on the starboard bow. Only available if the source ship has a valid heading.
        /// </summary>
        public Angle? RelativeDirection { get; init; }

    }
}

#pragma warning disable 1591
namespace Units
{
    public struct Distance
    {
        public const double MetersToNauticalMiles = 1.0 / 1852.0;
        public const double MetersToFeet = 1.0 / 0.3048;
        public const double MetersToFathoms = 1.0 / 1.852; // 1 / 1000 of a nautical mile
        private double _meters;

        private Distance(double meters)
        {
            _meters = meters;
        }

        public static Distance Zero
        {
            get
            {
                return Distance.FromMeters(0);
            }
        }

        public double Meters
        {
            get
            {
                return _meters;
            }
        }

        public double NauticalMiles
        {
            get
            {
                return _meters * MetersToNauticalMiles;
            }
        }

        public double Feet
        {
            get
            {
                return _meters * MetersToFeet;
            }
        }

        public double Fathoms
        {
            get
            {
                return _meters * MetersToFathoms;
            }
        }

        public static Distance FromMeters(double meters)
        {
            return new Distance(meters);
        }

        public static Distance FromNauticalMiles(double nm)
        {
            return new Distance(nm / MetersToNauticalMiles);
        }

        public static Distance FromFeet(double ft)
        {
            return new Distance(ft / MetersToFeet);
        }

        public static Distance FromFathoms(double fathoms)
        {
            return new Distance(fathoms / MetersToFathoms);
        }
    }
}

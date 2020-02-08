#pragma warning disable 1591
namespace Units
{
    public struct Distance
    {
        public const double MetersToNauticalMiles = 1.0 / 1852.0;
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

        public static Distance FromMeters(double meters)
        {
            return new Distance(meters);
        }

        public static Distance FromNauticalMiles(double nm)
        {
            return new Distance(nm / MetersToNauticalMiles);
        }
    }
}

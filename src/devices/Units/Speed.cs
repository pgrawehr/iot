using System;
using System.Collections.Generic;
using System.Text;
using Iot.Units;

namespace Units
{
    /// <summary>
    /// This represents speed in one direction
    /// </summary>
    public struct Speed
    {
        private const double MetersPerSecondToKnots = 1.944012;
        private const double MetersPerSecondToKmH = 3.6;
        private readonly double _ms;

        private Speed(double metersPerSecond)
        {
            _ms = metersPerSecond;
        }

        /// <summary>
        /// Speed in Meters Per Second
        /// </summary>
        public double MetersPerSecond => _ms;

        /// <summary>
        /// Speed in Knots
        /// </summary>
        public double Knots => _ms * MetersPerSecondToKnots;

        /// <summary>
        /// Speed in Kilometers Per Hour
        /// </summary>
        public double KilometersPerHour => _ms * MetersPerSecondToKmH;

        /// <summary>
        /// Creates Speed instance from knots
        /// </summary>
        /// <param name="value">Speed in Knots</param>
        /// <returns>Speed instance</returns>
        public static Speed FromKnots(double value)
        {
            return new Speed(value / MetersPerSecondToKnots);
        }

        /// <summary>
        /// Creates a speed instance from meters
        /// </summary>
        /// <param name="value">Meters per second</param>
        /// <returns></returns>
        public static Speed FromMetersPerSecond(double value)
        {
            return new Speed(value);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    /// <summary>
    /// Denotes a type of sensor source, identified mostly by its name.
    /// Every sensor source should only be constructed once.
    /// It should denote a sensor position and eventually a measured medium, but not its unit or physical type (as this
    /// will be derived from the quantity later assigned to the <see cref="SensorMeasurement"/>.
    /// Many useful sources are pre-defined as static members.
    /// </summary>
    public sealed class SensorSource : IEquatable<SensorSource>
    {
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="englishName">Name of the source in english</param>
        public SensorSource(string englishName)
        {
            Name = englishName;
        }

        /// <summary>
        /// Name of the source in english
        /// </summary>
        public string Name
        {
            get;
        }

        public static readonly SensorSource Air = new SensorSource("Air");
        public static readonly SensorSource Engine = new SensorSource("Engine, generic");
        public static readonly SensorSource EngineOil = new SensorSource("Engine, Oil");
        public static readonly SensorSource EngineWater = new SensorSource("Engine, Water");
        public static readonly SensorSource WindRelative = new SensorSource("Wind, relative angle"); // Ship relative
        public static readonly SensorSource WindTrue = new SensorSource("Wind, true angle"); // Ship relative angle
        public static readonly SensorSource WindAbsolute = new SensorSource("Wind, geographic angle"); // geographic angle

        /// <summary>
        /// Use i.e. for speed trough water or water temperature
        /// </summary>
        public static readonly SensorSource WaterRelative = new SensorSource("Water, Relative");

        /// <summary>
        /// Use for water temperature (alternative) or for absolute water speed (estimated or from tidal prediction)
        /// </summary>
        public static readonly SensorSource WaterAbsolute = new SensorSource("Water, Absolute");
        public static readonly SensorSource MainPower = new SensorSource("Main Power Source");
        public static readonly SensorSource AuxiliaryPower = new SensorSource("Auxiliary power Source");
        public static readonly SensorSource Fuel = new SensorSource("Fuel");
        public static readonly SensorSource Sewage = new SensorSource("Sewage");
        public static readonly SensorSource Freshwater = new SensorSource("Freshwater");

        public static readonly SensorSource Position = new SensorSource("Position");

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is SensorSource))
            {
                return false;
            }

            SensorSource other = (SensorSource)obj;
            return Name == other.Name;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public bool Equals(SensorSource other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Name == other.Name;
        }

        public static bool operator ==(SensorSource left, SensorSource right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SensorSource left, SensorSource right)
        {
            return !Equals(left, right);
        }
    }
}

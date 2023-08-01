// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        /// <param name="name">Name of the source</param>
        public SensorSource(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Name of the source in english
        /// </summary>
        public string Name
        {
            get;
        }

        public static readonly SensorSource Ais = new SensorSource("AIS");

        public static readonly SensorSource Air = new SensorSource("Air");
        public static readonly SensorSource Engine = new SensorSource("Engine");
        public static readonly SensorSource Wind = new SensorSource("Wind");

        /// <summary>
        /// Use i.e. for speed trough water or water temperature
        /// </summary>
        public static readonly SensorSource Water = new SensorSource("Water");

        public static readonly SensorSource MainPower = new SensorSource("Main Power Source");
        public static readonly SensorSource AuxiliaryPower = new SensorSource("Auxiliary power Source");
        public static readonly SensorSource Fuel = new SensorSource("Fuel");
        public static readonly SensorSource Sewage = new SensorSource("Sewage");
        public static readonly SensorSource Freshwater = new SensorSource("Freshwater");

        /// <summary>
        /// Position sensor (typically any GNSS receiver)
        /// </summary>
        public static readonly SensorSource Position = new SensorSource("Position");
        public static readonly SensorSource Compass = new SensorSource("Compass");

        /// <summary>
        /// User input source (i.e. buttons)
        /// </summary>
        public static readonly SensorSource UserInput = new SensorSource("User input");
        public static readonly SensorSource Cpu = new SensorSource("Cpu");

        /// <summary>
        /// Navigation system (e.g. waypoints, distance to target)
        /// </summary>
        public static readonly SensorSource Navigation = new SensorSource("Navigation");

        /// <inheritdoc/>
        public override bool Equals(object? obj)
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

        public bool Equals(SensorSource? other)
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

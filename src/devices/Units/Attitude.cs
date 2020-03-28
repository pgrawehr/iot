using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591
namespace Units
{
    /// <summary>
    /// Represents an attitude (orientation in space) of an object,
    /// represented by three euler angles.
    /// The angles shall be applied in heading -> pitch -> roll order (from world to object coordinates)
    /// </summary>
    public sealed class Attitude : IEquatable<Attitude>, ICloneable
    {
        public Attitude()
        {
        }

        public Attitude(Attitude attitude)
            : this(attitude.Pitch, attitude.Roll, attitude.Heading, attitude.IsTrueNorth)
        {
            IsTrueNorth = true;
        }

        public Attitude(double pitch, double roll, double heading)
        {
            IsTrueNorth = true;
            Pitch = Angle.FromDegrees(pitch);
            Roll = Angle.FromDegrees(roll);
            Heading = Angle.FromDegrees(heading);
        }

        public Attitude(double pitch, double roll, double heading, bool trueNorth)
        {
            IsTrueNorth = trueNorth;

            Pitch = Angle.FromDegrees(pitch);
            Roll = Angle.FromDegrees(roll);
            Heading = Angle.FromDegrees(heading);
        }

        public Attitude(Angle pitch, Angle roll, Angle heading, bool trueNorth)
        {
            IsTrueNorth = trueNorth;

            Pitch = pitch;
            Roll = roll;
            Heading = heading;
        }

        public Angle Heading
        {
            get;
        }

        public bool IsTrueNorth
        {
            get;
        }

        public Angle Pitch
        {
            get;
        }

        public Angle Roll
        {
            get;
        }

        public static bool operator ==(Attitude a, Attitude b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(Attitude a, Attitude b)
        {
            if (ReferenceEquals(a, b))
            {
                return false;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return true;
            }

            return !a.Equals(b);
        }

        object ICloneable.Clone()
        {
            return new Attitude(Pitch, Roll, Heading, IsTrueNorth);
        }

        public Attitude Clone()
        {
            return new Attitude(Pitch, Roll, Heading, IsTrueNorth);
        }

        private string Print()
        {
            double heading = Heading.Degrees;
            double pitch = Pitch.Degrees;
            double roll = Roll.Degrees;
            return string.Concat("Roll ", roll.ToString("f1"), "\x00b0, Pitch ", pitch.ToString("f1"), "\x00b0, Hdg ", heading.ToString("f1"), "\x00b0");
        }

        public override bool Equals(object obj)
        {
            Attitude other = obj as Attitude;
            if (other == null)
            {
                return false;
            }

            return Heading == other.Heading && Pitch == other.Pitch && Roll == other.Roll && IsTrueNorth == other.IsTrueNorth;
        }

        public bool Equals(Attitude other)
        {
            if (other == null)
            {
                return false;
            }

            return Heading == other.Heading && Pitch == other.Pitch && Roll == other.Roll && IsTrueNorth == other.IsTrueNorth;
        }

        public override int GetHashCode()
        {
            return Heading.GetHashCode() ^ Pitch.GetHashCode() ^ Roll.GetHashCode() ^ (IsTrueNorth ? 1 : 2);
        }

        public override string ToString()
        {
            return Print();
        }

        public string ToString(string format)
        {
            return ToString(format, null);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            double heading = Heading.Degrees;
            double pitch = Pitch.Degrees;
            double roll = Roll.Degrees;
            return string.Concat("Roll ", roll.ToString(format, formatProvider), "\x00b0, Pitch ", pitch.ToString(format, formatProvider), "\x00b0, Hdg ", heading.ToString(format, formatProvider), "\x00b0");
        }
    }
}

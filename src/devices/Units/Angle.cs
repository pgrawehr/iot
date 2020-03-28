using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Units
{
    /// <summary>
    /// Represents an angle value (i.e. for track or heading data)
    /// </summary>
    public struct Angle : IEquatable<Angle>
    {
        private const double EPSILON = 1E-15;

        /// <summary>
        /// A zeroed angle
        /// </summary>
        public static readonly Angle Zero = FromRadians(0);

        /// <summary>
        /// Angle containing Not a Number
        /// </summary>
        public static readonly Angle NaN = FromRadians(double.NaN);

        private readonly double _angleRadians;

        /// <summary>
        /// Default ctor
        /// </summary>
        /// <param name="radians">Initial angle in radians</param>
        public Angle(double radians)
        {
            _angleRadians = radians;
        }

        /// <summary>
        /// Angle in radians
        /// </summary>
        public double Radians
        {
            get
            {
                return _angleRadians;
            }
        }

        /// <summary>
        /// Angle in Degrees
        /// </summary>
        public double Degrees
        {
            get
            {
                return _angleRadians / Math.PI * 180;
            }
        }

        /// <summary>
        /// Constructs an instance from radians
        /// </summary>
        public static Angle FromRadians(double radians)
        {
            return new Angle(radians);
        }

        /// <summary>
        /// Constructs an instance from a value in degrees
        /// </summary>
        public static Angle FromDegrees(double degrees)
        {
            return new Angle(degrees / 180 * Math.PI);
        }

        /// <summary>
        /// Compares the two instances for near-equality
        /// </summary>
        public static bool AlmostEqual(Angle a, Angle b, double epsilon)
        {
            return Math.Abs(a.Radians - b.Radians) < epsilon;
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        public static bool operator ==(Angle a, Angle b)
        {
            return Math.Abs(a.Radians - b.Radians) < EPSILON;
        }

        /// <summary>
        /// Unequality operator
        /// </summary>
        public static bool operator !=(Angle a, Angle b)
        {
            return Math.Abs(a.Radians - b.Radians) > EPSILON;
        }

        /// <summary>
        /// Less-than operator
        /// </summary>
        public static bool operator <(Angle a, Angle b)
        {
            return a.Radians < b.Radians;
        }

        /// <summary>
        /// Greater operator
        /// </summary>
        public static bool operator >(Angle a, Angle b)
        {
            return a.Radians > b.Radians;
        }

        /// <summary>
        /// Greater-or-equal operator
        /// </summary>
        public static bool operator >=(Angle a, Angle b)
        {
            return a.Radians >= b.Radians;
        }

        /// <summary>
        /// Less-or-equal operator
        /// </summary>
        public static bool operator <=(Angle a, Angle b)
        {
            return a.Radians <= b.Radians;
        }

        /// <summary>
        /// Binary addition
        /// </summary>
        public static Angle operator +(Angle a, Angle b)
        {
            double res = a.Radians + b.Radians;
            return FromRadians(res);
        }

        /// <summary>
        /// Binary minus operator
        /// </summary>
        public static Angle operator -(Angle a, Angle b)
        {
            double res = a.Radians - b.Radians;
            return FromRadians(res);
        }

        /// <summary>
        /// Multiplication
        /// </summary>
        public static Angle operator *(Angle a, double times)
        {
            return FromRadians(a.Radians * times);
        }

        /// <summary>
        /// Multiplication
        /// </summary>
        public static Angle operator *(double times, Angle a)
        {
            return FromRadians(a.Radians * times);
        }

        /// <summary>
        /// Division
        /// </summary>
        public static Angle operator /(Angle a, double divisor)
        {
            return FromRadians(a.Radians / divisor);
        }

        /// <summary>
        /// Unary - operator
        /// </summary>
        public static Angle operator -(Angle a)
        {
            return new Angle(-a.Radians);
        }

        /// <inheritdoc cref="object"/>
        public override int GetHashCode()
        {
            return (int)(Radians * 100000);
        }

        /// <summary>
        /// Normalizes the angle so it is between 0° and 360° or between -180° and +180° respectively.
        /// </summary>
        /// <param name="to360">Set to true to normalize to 0-360°, otherwise normalizes to +/-180°</param>
        public Angle Normalize(bool to360)
        {
            double r = Radians;
            if (to360)
            {
                if (r > Math.PI * 2)
                {
                    r = r % (Math.PI * 2);
                }

                if (r < 0)
                {
                    r = -(Math.Abs(r) % (Math.PI * 2));
                    if (r < 0)
                    {
                        r += Math.PI * 2;
                    }
                }
            }
            else
            {
                if (r > Math.PI)
                {
                    r = r % (Math.PI * 2);
                    if (r > Math.PI)
                    {
                        // Still above 180?
                        r -= Math.PI * 2;
                    }
                }

                if (r < -Math.PI)
                {
                    r = -(Math.Abs(r) % (Math.PI * 2));
                    if (r < -Math.PI)
                    {
                        r += Math.PI * 2;
                    }
                }
            }

            return new Angle(r);
        }

        /// <summary>
        /// Converts degrees to degrees/minutes/seconds
        /// </summary>
        /// <param name="withSign">True if a - sign should be printed for negative numbers. Use
        /// false if you want to prefix or postfix the angle with a cardinal direction (N/E/S/W)</param>
        /// <returns>String on format dd° mm' ss.sss"</returns>
        public string ToStringDms(bool withSign)
        {
            double decimalDegrees = Degrees;
            double d = Math.Abs(decimalDegrees);
            double m = (60 * (d - Math.Floor(d)));
            double s = (60 * (m - Math.Floor(m)));

            return String.Format("{0}{1}° {2}' {3:f3}\"",
                withSign && Radians < 0 ? "-" : string.Empty,
                (int)d,
                (int)m,
                s);
        }

        /// <inheritdoc cref="IEquatable{T}"/>
        public bool Equals(Angle other)
        {
            return (Math.Abs(Radians - other.Radians) < EPSILON);
        }

        /// <inheritdoc cref="object"/>
        public override bool Equals(object other)
        {
            if (!(other is Angle))
            {
                return false;
            }

            return Equals((Angle)other);
        }

        /// <summary>
        /// String representation of the current angle, in degrees
        /// </summary>
        public override string ToString()
        {
            return Degrees.ToString(CultureInfo.InvariantCulture) + "°";
        }

        /// <summary>
        /// String representation with formatting argument
        /// </summary>
        public string ToString(string format)
        {
            return Degrees.ToString(format);
        }

        /// <summary>
        /// String representation with formatting argument
        /// </summary>
        public string ToString(string format, IFormatProvider provider)
        {
            return Degrees.ToString(format, provider);
        }

        /// <summary>
        /// String representation with formatting argument
        /// </summary>
        public string ToString(IFormatProvider provider)
        {
            return Degrees.ToString(provider);
        }
    }
}

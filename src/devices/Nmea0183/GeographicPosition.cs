// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// Represents a position in WGS84 coordinates.
    /// </summary>
    [Serializable]
    public sealed class GeographicPosition : ICloneable, IEquatable<GeographicPosition>
    {
        private const string DegreesSymbol = "°";
        private const string MinutesSymbol = "\'";
        private const string SecondsSymbol = "\"";
        private const double ComparisonEpsilon = 1E-8; // degrees (around 1 cm near the equator)
        private readonly double _latitude;
        private readonly double _longitude;
        private readonly double _height;

        public GeographicPosition()
        {
            _latitude = _longitude = _height = 0;
        }

        public GeographicPosition(GeographicPosition pos)
        {
            _latitude = pos.Latitude;
            _longitude = pos.Longitude;
            _height = pos.EllipsoidalHeight;
        }

        public GeographicPosition(double latitude, double longitude, double ellipsoidalHeight)
        {
            _latitude = latitude;
            _longitude = longitude;
            _height = ellipsoidalHeight;
        }

        public double EllipsoidalHeight
        {
            get
            {
                return _height;
            }
        }

        public double Latitude
        {
            get
            {
                return _latitude;
            }
        }

        public double Longitude
        {
            get
            {
                return _longitude;
            }
        }

        public static void GetDegreesMinutesSeconds(double angle, int secDigits, out double normalizedVal, out double degrees, out double minutes, out double seconds)
        {
            angle = PositionExtensions.NormalizeAngleTo180(angle);
            normalizedVal = angle;
            angle = Math.Abs(angle);
            degrees = Math.Floor(angle);

            double remains = (angle - degrees) * 60.0;
            minutes = Math.Floor(remains);

            seconds = (remains - minutes) * 60.0;

            // If rounding the seconds to the given digit would print out "60",
            // add 1 to the minutes instead
            if (Math.Round(seconds, secDigits) >= 60.0)
            {
                minutes += 1;
                seconds = 0;
                if (minutes >= 60) // is basically an integer at this point
                {
                    degrees += 1;
                    minutes = 0;
                    if (degrees >= 360)
                    {
                        degrees -= 360;
                    }
                }
            }
        }

        private static string GetEastOrWest(double sign)
        {
            if (sign >= 0 && sign <= 180)
            {
                return "E";
            }

            return "W";
        }

        private static string GetNorthOrSouth(double sign)
        {
            if (sign >= 0)
            {
                return "N";
            }

            return "S";
        }

        public static string GetLongitudeString(double longitude)
        {
            object[] args = new object[7];
            GetDegreesMinutesSeconds(longitude, 2, out var normalizedVal, out var deg, out var min, out var sec);
            string strEastOrWest = GetEastOrWest(normalizedVal);

            args[0] = deg;
            args[1] = DegreesSymbol;
            args[2] = min;
            args[3] = MinutesSymbol;
            args[4] = sec.ToString("00.00");
            args[5] = SecondsSymbol;
            args[6] = strEastOrWest;
            string strLonRet = string.Format(CultureInfo.InvariantCulture, "{0}{1} {2:00}{3} {4}{5}{6}", args);
            return strLonRet;
        }

        public static string GetLatitudeString(double latitude)
        {
            object[] args = new object[7];

            GetDegreesMinutesSeconds(latitude, 2, out var normalizedVal, out var deg, out var min, out var sec);
            string strNorthOrSouth = GetNorthOrSouth(normalizedVal);

            args[0] = deg;
            args[1] = DegreesSymbol;
            args[2] = min;
            args[3] = MinutesSymbol;
            args[4] = sec.ToString("00.00");
            args[5] = SecondsSymbol;
            args[6] = strNorthOrSouth;
            string strLatRet = string.Format(CultureInfo.InvariantCulture, "{0}{1} {2:00}{3} {4}{5}{6}", args);
            return strLatRet;
        }

        object ICloneable.Clone()
        {
            return new GeographicPosition(this);
        }

        public GeographicPosition Clone()
        {
            return new GeographicPosition(this);
        }

        public bool ContainsValidPosition()
        {
            if (((Latitude == 0.0) && (Longitude == 0.0)) && (EllipsoidalHeight == 0.0))
            {
                return false;
            }

            if (double.IsNaN(Latitude) || double.IsNaN(Longitude))
            {
                return false;
            }

            if ((Math.Abs(Latitude) > 90.0) || (Math.Abs(Longitude) > 360.0))
            {
                return false;
            }

            return true;
        }

        public bool EqualPosition(GeographicPosition position)
        {
            if (position == null)
            {
                return false;
            }

            bool ret;
            if ((Math.Abs((position.Longitude - Longitude)) < ComparisonEpsilon) && (Math.Abs(position.Latitude - Latitude) < ComparisonEpsilon))
            {
                ret = true;
            }
            else
            {
                ret = false;
            }

            return ret;
        }

        public override bool Equals(object? obj)
        {
            GeographicPosition? position = obj as GeographicPosition;

            if (position == null)
            {
                return false;
            }

            if (((Math.Abs(position.Longitude - Longitude) < ComparisonEpsilon) &&
                (Math.Abs(position.Latitude - Latitude) < ComparisonEpsilon)) &&
                (Math.Abs(position.EllipsoidalHeight - EllipsoidalHeight) < ComparisonEpsilon))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Equals(GeographicPosition? position)
        {
            if (position == null)
            {
                return false;
            }

            if (((Math.Abs(position.Longitude - Longitude) < ComparisonEpsilon) &&
                 (Math.Abs(position.Latitude - Latitude) < ComparisonEpsilon)) &&
                (Math.Abs(position.EllipsoidalHeight - EllipsoidalHeight) < ComparisonEpsilon))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            if (Double.IsNaN(Latitude) || Double.IsNaN(Longitude))
            {
                return "NaN";
            }

            if (Double.IsInfinity(Latitude) || Double.IsInfinity(Longitude))
            {
                return "Infinity";

            }

            var strLatRet = GetLatitudeString(Latitude);
            var strLonRet = GetLongitudeString(Longitude);

            return string.Concat(strLatRet, " / ", strLonRet, " Ellipsoidal Height: ", EllipsoidalHeight.ToString("F0"));
        }

        public override int GetHashCode()
        {
            return Latitude.GetHashCode() ^ Longitude.GetHashCode() ^ EllipsoidalHeight.GetHashCode() ^ 0x7a2b;
        }
    }
}

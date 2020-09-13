using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    internal enum NoUnitEnum
    {
        None,
    }

    /// <summary>
    /// An object that can store an arbitrary type as IQuantity.
    /// Many properties will not deliver meaningful, but the class is written to always return something and avoid exceptions, so
    /// it can be used in data binding.
    /// </summary>
    /// <typeparam name="T">Any data type</typeparam>
    public class CustomQuantity<T> : IQuantity
    {
        public static CustomQuantity<T> Zero = new CustomQuantity<T>(default(T));

        private T _value;

        public CustomQuantity(T value)
        {
            _value = value;
        }

        public QuantityType Type
        {
            get
            {
                return QuantityType.Undefined;
            }
        }

        public BaseDimensions Dimensions
        {
            get
            {
                return BaseDimensions.Dimensionless;
            }
        }

        public QuantityInfo QuantityInfo
        {
            get
            {
                return new QuantityInfo(Type, new UnitInfo[] { new UnitInfo(NoUnitEnum.None, BaseUnits.Undefined) }, NoUnitEnum.None,
                    Zero, BaseDimensions.Dimensionless);
            }
        }

        public Enum Unit
        {
            get
            {
                return NoUnitEnum.None;
            }
        }

        /// <summary>
        /// Note: Not supposed to be called.
        /// </summary>
        double IQuantity.Value
        {
            get
            {
                return 0;
            }
        }

        public T Value
        {
            get
            {
                return _value;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            // This has no unit, so ignore the request to format as unit abbreviation
            if (format.StartsWith("a", StringComparison.InvariantCultureIgnoreCase))
            {
                return string.Empty;
            }

            if (_value is IFormattable fmt)
            {
                return fmt.ToString(format, formatProvider);
            }
            else
            {
                return _value.ToString();
            }
        }

        public double As(Enum unit)
        {
            return 0;
        }

        public double As(UnitSystem unitSystem)
        {
            return 0;
        }

        public IQuantity ToUnit(Enum unit)
        {
            // Becuse there are no other units
            return this;
        }

        public IQuantity ToUnit(UnitSystem unitSystem)
        {
            return this;
        }

        /// <summary>
        /// These are obsolete, anyway
        /// </summary>
        public string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        public string ToString(IFormatProvider provider, int significantDigitsAfterRadix)
        {
            return ToString();
        }

        public string ToString(IFormatProvider provider, string format, params object[] args)
        {
            return String.Format(provider, format, args);
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}

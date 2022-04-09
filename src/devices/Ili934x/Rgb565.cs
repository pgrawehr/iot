// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;

namespace Iot.Device.Ili934x
{
    /// <summary>
    /// This is the image format used by the Ili934X internally
    /// It is similar to the default format <see cref="Bgr565"/>, but uses a different byte ordering
    /// </summary>
    internal struct Rgb565 : IPixel<Rgb565>, IEquatable<Rgb565>
    {
        private ushort _value;

        public Rgb565(ushort packedValue)
        {
            _value = packedValue;
        }

        public Rgb565(ushort r, ushort g, ushort b)
        {
            _value = 0;
            InitFrom(r, g, b);
        }

        private void InitFrom(ushort r, ushort g, ushort b)
        {
            UInt16 retval = (UInt16)(r >> 3);
            // shift right to make room for the green Value
            retval <<= 6;
            // combine with the 6 MSB if the green value
            retval |= (UInt16)(g >> 2);
            // shift right to make room for the red or blue Value
            retval <<= 5;
            // combine with the 6 MSB if the red or blue value
            retval |= (UInt16)(b >> 3);
            _value = retval;
        }

        /// <summary>
        /// Convert a color structure to a byte tuple representing the colour in 565 format.
        /// </summary>
        /// <param name="color">The color to be converted.</param>
        /// <returns>
        /// This method returns the low byte and the high byte of the 16bit value representing RGB565 or BGR565 value
        ///
        /// byte    11111111 00000000
        /// bit     76543210 76543210
        ///
        /// For ColorSequence.RGB
        ///         RRRRRGGG GGGBBBBB
        ///         43210543 21043210
        ///
        /// For ColorSequence.BGR
        ///         BBBBBGGG GGGRRRRR
        ///         43210543 21043210
        /// </returns>
        public static Rgb565 FromRgba32(Rgba32 color)
        {
            // get the top 5 MSB of the blue or red value
            UInt16 retval = (UInt16)(color.R >> 3);
            // shift right to make room for the green Value
            retval <<= 6;
            // combine with the 6 MSB if the green value
            retval |= (UInt16)(color.G >> 2);
            // shift right to make room for the red or blue Value
            retval <<= 5;
            // combine with the 6 MSB if the red or blue value
            retval |= (UInt16)(color.B >> 3);

            return new Rgb565((ushort)(retval >> 8 | retval << 8));
        }

        public ushort PackedValue
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        public PixelOperations<Rgb565> CreatePixelOperations()
        {
            throw new NotImplementedException();
        }

        public void FromScaledVector4(Vector4 vector)
        {
            throw new NotImplementedException();
        }

        public Vector4 ToScaledVector4()
        {
            throw new NotImplementedException();
        }

        public void FromVector4(Vector4 vector)
        {
            throw new NotImplementedException();
        }

        public Vector4 ToVector4()
        {
            throw new NotImplementedException();
        }

        public void FromArgb32(Argb32 source)
        {
            InitFrom(source.R, source.G, source.B);
        }

        public void FromBgra5551(Bgra5551 source)
        {
            throw new NotImplementedException();
        }

        public void FromBgr24(Bgr24 source)
        {
            throw new NotImplementedException();
        }

        public void FromBgra32(Bgra32 source)
        {
            throw new NotImplementedException();
        }

        public void FromAbgr32(Abgr32 source)
        {
            throw new NotImplementedException();
        }

        public void FromL8(L8 source)
        {
            throw new NotImplementedException();
        }

        public void FromL16(L16 source)
        {
            throw new NotImplementedException();
        }

        public void FromLa16(La16 source)
        {
            throw new NotImplementedException();
        }

        public void FromLa32(La32 source)
        {
            throw new NotImplementedException();
        }

        public void FromRgb24(Rgb24 source)
        {
            throw new NotImplementedException();
        }

        void IPixel.FromRgba32(Rgba32 source)
        {
            throw new NotImplementedException();
        }

        public void ToRgba32(ref Rgba32 dest)
        {
            throw new NotImplementedException();
        }

        public void FromRgb48(Rgb48 source)
        {
            throw new NotImplementedException();
        }

        public void FromRgba64(Rgba64 source)
        {
            throw new NotImplementedException();
        }

        public bool Equals(Rgb565 other)
        {
            return _value == other._value;
        }

        public override bool Equals(object? obj)
        {
            return obj is Rgb565 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public static bool operator ==(Rgb565 left, Rgb565 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rgb565 left, Rgb565 right)
        {
            return !left.Equals(right);
        }
    }
}

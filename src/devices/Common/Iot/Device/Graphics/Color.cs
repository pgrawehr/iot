// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS1591
namespace Iot.Device.Graphics
{
    /// <summary>
    /// Represents colors
    /// </summary>
    public struct Color
    {
        public static Color Black = new Color();
        public static Color White = new Color(255, 255, 255);
        public static Color Red = new Color(255, 0, 0);
        public static Color Blue = new Color(0, 0, 255);
        public static Color Green = new Color(0, 255, 0);

        public Color(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
            RBits = 8;
            GBits = 8;
            BBits = 8;
        }

        public int R
        {
            get;
            set;
        }

        public int G
        {
            get;
            set;
        }

        public int B
        {
            get;
            set;
        }

        public byte RBits
        {
            get;
            init;
        }

        public byte GBits
        {
            get;
            init;
        }

        public byte BBits
        {
            get;
            init;
        }

        public string ToHex()
        {
            if (RBits == 8 && BBits == 8 && GBits == 8)
            {
                return $"{R:X2}{G:X2}{B:X2}";
            }

            throw new NotSupportedException("Conversion to hex is only available for 32bit colors");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Avalonia.Media;

namespace DisplayControl
{
    public class SystemDrawing
    {
        public static Avalonia.Media.Color FromColor(System.Drawing.Color inColor)
        {
            return new Avalonia.Media.Color(inColor.A, inColor.R, inColor.G, inColor.B);
        }

        public static Avalonia.Media.Color FromName(string name)
        {
            return FromColor(System.Drawing.Color.FromName(name));
        }
    }
}

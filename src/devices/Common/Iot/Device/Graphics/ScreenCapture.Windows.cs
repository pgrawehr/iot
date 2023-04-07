// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Graphics
{
    public partial class ScreenCapture
    {
        [SuppressMessage("Interoperability", "CA1416", Justification = "Only used on windows, see call site")]
        private static BitmapImage? GetScreenContentsWindows(Rectangle area)
        {
            try
            {
                using (Bitmap bitmap = new Bitmap(area.Width, area.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(new System.Drawing.Point(area.Left, area.Top), System.Drawing.Point.Empty, new System.Drawing.Size(area.Width, area.Height));
                    }

                    var image = Converters.ToBitmapImage(bitmap);
                    // For some reason, we need to swap R and B here. Strange...
                    Converters.ColorTransform(image, (i, j, c) => Color.FromArgb(c.B, c.G, c.R, c.A));
                    return image;
                }
            }
            catch (Win32Exception)
            {
                return null;
            }
        }

        private Rectangle ScreenSizeWindows()
        {
            return new Rectangle(Interop.GetSystemMetrics(Interop.SystemMetric.SM_XVIRTUALSCREEN),
                Interop.GetSystemMetrics(Interop.SystemMetric.SM_YVIRTUALSCREEN),
                Interop.GetSystemMetrics(Interop.SystemMetric.SM_CXVIRTUALSCREEN),
                Interop.GetSystemMetrics(Interop.SystemMetric.SM_CYVIRTUALSCREEN));
        }
    }
}

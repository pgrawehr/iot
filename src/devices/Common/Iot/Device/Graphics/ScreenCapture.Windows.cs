using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Iot.Device.Graphics
{
    public partial class ScreenCapture
    {
        private Image<Rgba32>? GetScreenContentsWindows(SixLabors.ImageSharp.Rectangle area)
        {
            try
            {
                using (Bitmap bitmap = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(new System.Drawing.Point(area.Left, area.Top), System.Drawing.Point.Empty, new System.Drawing.Size(area.Width, area.Height));
                    }

                    var image = Converters.ToImage(bitmap);
                    // For some reason, we need to swap R and B here. Strange...
                    Converters.ColorTransform(image, (i, j, c) => new Rgba32(c.B, c.G, c.R, c.A));
                    return image;
                }
            }
            catch (Win32Exception)
            {
                return null;
            }
        }

        private SixLabors.ImageSharp.Rectangle ScreenSizeWindows()
        {
            return new SixLabors.ImageSharp.Rectangle(Interop.GetSystemMetrics(Interop.SystemMetric.SM_XVIRTUALSCREEN),
                Interop.GetSystemMetrics(Interop.SystemMetric.SM_YVIRTUALSCREEN),
                Interop.GetSystemMetrics(Interop.SystemMetric.SM_CXVIRTUALSCREEN),
                Interop.GetSystemMetrics(Interop.SystemMetric.SM_CYVIRTUALSCREEN));
        }
    }
}

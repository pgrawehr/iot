using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Iot.Device.Graphics
{
    /// <summary>
    /// A class that takes screenshots.
    /// </summary>
    public static class ScreenCapture
    {
        /// <summary>
        /// Gets the contents of a section of the screen
        /// </summary>
        /// <returns>An image</returns>
        public static Image<Rgba32> GetScreenContents(SixLabors.ImageSharp.Rectangle area)
        {
            using (Bitmap bitmap = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb))
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(new System.Drawing.Point(area.Left, area.Top), System.Drawing.Point.Empty, new System.Drawing.Size(area.Width, area.Height));
                }

                var image = Converters.ToImage(bitmap);
                // For some reason, we need to swap R and B here. Strange...
                Converters.ColorTransform(image, (i, c) => new Rgba32(c.B, c.G, c.R, c.A));
                return image;
            }
        }

        /// <summary>
        /// Gets the contents of the screen
        /// </summary>
        /// <returns>An image</returns>
        public static Image<Rgba32> GetScreenContents()
        {
            return GetScreenContents(ScreenSize());
        }

        /// <summary>
        /// Returns the size of the virtual desktop.
        /// This returns the full size of the virtual desktop, which might span multiple screens
        /// </summary>
        /// <returns>A rectangle with the size of all screens</returns>
        public static SixLabors.ImageSharp.Rectangle ScreenSize()
        {
            return new SixLabors.ImageSharp.Rectangle(Interop.GetSystemMetrics(Interop.SystemMetric.SM_XVIRTUALSCREEN),
                Interop.GetSystemMetrics(Interop.SystemMetric.SM_YVIRTUALSCREEN),
                Interop.GetSystemMetrics(Interop.SystemMetric.SM_CXVIRTUALSCREEN),
                Interop.GetSystemMetrics(Interop.SystemMetric.SM_CYVIRTUALSCREEN));
        }
    }
}

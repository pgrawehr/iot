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
    public partial class ScreenCapture : IDisposable
    {
        /// <summary>
        /// Creates a new instance of the ScreenCapture class
        /// </summary>
        public ScreenCapture()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                InitLinux();
            }
        }

        /// <summary>
        /// Gets the contents of a section of the screen
        /// </summary>
        /// <returns>An image. Returns null if no image can currently be retrieved (may happen e.g. when the safe desktop is shown)</returns>
        public virtual Image<Rgba32>? GetScreenContents(SixLabors.ImageSharp.Rectangle area)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return GetScreenContentsWindows(area);
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return GetScreenContentsLinux(area);
            }

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Gets the contents of the screen
        /// </summary>
        /// <returns>An image. Returns null if no image can currently be retrieved (may happen e.g. when the safe desktop is shown)</returns>
        public Image<Rgba32>? GetScreenContents()
        {
            return GetScreenContents(ScreenSize());
        }

        /// <summary>
        /// Returns the size of the virtual desktop.
        /// This returns the full size of the virtual desktop, which might span multiple screens
        /// </summary>
        /// <returns>A rectangle with the size of all screens</returns>
        public virtual SixLabors.ImageSharp.Rectangle ScreenSize()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return ScreenSizeWindows();
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return ScreenSizeLinux();
            }

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Cleans internal structures
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_display != IntPtr.Zero)
            {
                Interop.XCloseDisplay(_display);
                _display = IntPtr.Zero;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

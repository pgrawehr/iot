// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static Interop;

namespace Iot.Device.Graphics
{
    public partial class ScreenCapture
    {
        private IntPtr _display;

        private Configuration _imageConfiguration;

        private unsafe void InitLinux()
        {
            _display = XOpenDisplay();
            _imageConfiguration = Configuration.Default.Clone();
            _imageConfiguration.PreferContiguousImageBuffers = true;
            if (_display == IntPtr.Zero)
            {
                throw new NotSupportedException("Unable to open display");
            }
        }

        private unsafe Image<Rgba32> GetScreenContentsLinux(Rectangle area)
        {
            var root = XDefaultRootWindow(_display);

            IntPtr rawImage = XGetImage(_display, root, area.Left, area.Top, (UInt32)area.Width, (UInt32)area.Height, AllPlanes, ZPixmap);

            XImage? image = Marshal.PtrToStructure<XImage>(rawImage);

            if (image == null)
            {
                throw new NotSupportedException("Unable to get screen image pointer");
            }

            var resultImage = new Image<Rgba32>(_imageConfiguration, area.Width, area.Height);

            resultImage.DangerousTryGetSinglePixelMemory(out var memory);

            uint red_mask = image.red_mask;
            uint green_mask = image.green_mask;
            uint blue_mask = image.blue_mask;

            for (int x = 0; x < area.Width; x++)
            {
                for (int y = 0; y < area.Height; y++)
                {
                    UInt32 pixel = XGetPixel(image, x, y);

                    UInt32 blue = pixel & blue_mask;
                    UInt32 green = (pixel & green_mask) >> 8;
                    UInt32 red = (pixel & red_mask) >> 16;

                    var color = new Rgba32((byte)red, (byte)green, (byte)blue);
                    memory.Span[area.Width * y + x] = color;
                }
            }

            XDestroyImage(rawImage);
            return resultImage!;
        }

        private SixLabors.ImageSharp.Rectangle ScreenSizeLinux()
        {
            var root = XDefaultRootWindow(_display);
            Interop.XWindowAttributes gwa = default;

            XGetWindowAttributes(_display, root, ref gwa);
            int width = gwa.width;
            int height = gwa.height;
            return new Rectangle(0, 0, width, height);
        }

    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Graphics
{
    /// <summary>
    /// Contains a set of converters from <see cref="System.Drawing.Bitmap"/> to <see cref="SixLabors.ImageSharp.Image{T}"/>, for
    /// easy conversion of legacy code that still uses the obsolete System.Drawing library
    /// </summary>
    public static class Converters
    {
        /// <summary>
        /// Convert a <see cref="System.Drawing.Bitmap"/> to a <see cref="SixLabors.ImageSharp.Image{Rgba32}"/>
        /// </summary>
        /// <param name="bmp">Input bitmap</param>
        /// <returns>An image</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bmp"/> is null</exception>
        /// <exception cref="NotSupportedException">The input format is not supported</exception>
        public static unsafe Image<Rgba32> ToImage(Bitmap bmp)
        {
            if (bmp == null)
            {
                throw new ArgumentNullException(nameof(bmp));
            }

            if (bmp.PixelFormat == PixelFormat.Format32bppArgb)
            {
                var target = new Image<Rgba32>(bmp.Width, bmp.Height);

                var bmd = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

                for (int i = 0; i < bmp.Height; i++)
                {
                    IntPtr offsetSource = bmd.Scan0 + (bmd.Stride * i);
                    void* source = offsetSource.ToPointer();
                    target.ProcessPixelRows(x =>
                    {
                        Span<Rgba32> sourceSpan = new Span<Rgba32>(source, bmd.Width);
                        var targetSpan = x.GetRowSpan(i);
                        sourceSpan.CopyTo(targetSpan);
                    });
                }

                bmp.UnlockBits(bmd);

                return target;
            }

            throw new NotSupportedException($"Converting images of type {bmp.PixelFormat} is not supported");
        }

        /// <summary>
        /// Performs a color transformation on the image
        /// </summary>
        /// <param name="image">The image to transform</param>
        /// <param name="transformFunc">A function that is called for each pixel, taking the coordinate of the pixel in the whole image and the input color</param>
        public static void ColorTransform<T>(Image<T> image, Func<int, int, T, T> transformFunc)
            where T : unmanaged, IPixel<T>
        {
            Parallel.For(0, image.Height, j =>
            {
                for (int i = 0; i < image.Width; i++)
                {
                    image[i, j] = transformFunc(i, j, image[i, j]);
                }
            });
        }

        /// <summary>
        /// Adjusts the target position and size so that a given image can be copied to a rectangle of size destination
        /// </summary>
        /// <param name="image">The input image size</param>
        /// <param name="leftTop">[in, out] The top left corner of the input image to show. If at the bottom or right edge of the destination, this will be
        /// reset so that the right edge of the input image is at the right edge of the destination</param>
        /// <param name="destination">The destination rectangle. If this is smaller than the input image, it will be cropped</param>
        public static void AdjustImageDestination(Image<Rgba32> image, ref SixLabors.ImageSharp.Point leftTop, ref SixLabors.ImageSharp.Rectangle destination)
        {
            int left = leftTop.X;
            int top = leftTop.Y;

            if (destination.Width > image.Width)
            {
                // Rectangle is a struct, so this has no effect on the caller (yet)
                destination.Width = image.Width;
            }

            if (destination.Height > image.Height)
            {
                destination.Height = image.Height;
            }

            if (left < 0)
            {
                left = 0;
            }

            if (left > image.Width - destination.Width)
            {
                left = image.Width - destination.Width;
            }

            if (top < 0)
            {
                top = 0;
            }

            if (top > image.Height - destination.Height)
            {
                top = image.Height - destination.Height;
            }

            leftTop = new SixLabors.ImageSharp.Point(left, top);
            destination = new SixLabors.ImageSharp.Rectangle(destination.X, destination.Y, destination.Width, destination.Height);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
                    Span<Rgba32> sourceSpan = new Span<Rgba32>(source, bmd.Width);
                    sourceSpan.CopyTo(target.GetPixelRowSpan(i));
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
        /// <param name="transformFunc">A function that is called for each pixel, taking the index of the pixel in the whole image and the input color</param>
        public static void ColorTransform(Image<Rgba32> image, Func<int, Rgba32, Rgba32> transformFunc)
        {
            if (!image.TryGetSinglePixelSpan(out var span))
            {
                throw new InvalidOperationException("Unable to get pixel data");
            }

            for (int i = 0; i < span.Length; i++)
            {
                span[i] = transformFunc(i, span[i]);
            }
        }
    }
}

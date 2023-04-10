// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace Iot.Device.Graphics.SkiaSharpConnector
{
    /// <summary>
    /// Contains extension methods that operate on <see cref="IGraphics"/>
    /// </summary>
    public static class SkiaSharpGraphicsExtensions
    {
        /// <summary>
        /// Resizes an image. Extension method for SkiaSharp
        /// </summary>
        /// <param name="image">The image to resize</param>
        /// <param name="size">The new size</param>
        /// <returns>A new image. The old image is unaffected</returns>
        /// <exception cref="NotSupportedException">The image is not a SkiaSharpBitmap, meaning more than one image connector is in scope</exception>
        public static BitmapImage Resize(this BitmapImage image, Size size)
        {
            if (image is SkiaSharpBitmap img)
            {
                var resized = img.WrappedBitmap.Resize(new SKSizeI(size.Width, size.Height), SKFilterQuality.Medium);
                return new SkiaSharpBitmap(resized, image.PixelFormat);
            }

            throw new NotSupportedException("This overload can only Resize SkiaSharpImage instances");
        }

        /// <summary>
        /// Draws text to the bitmap
        /// </summary>
        public static void DrawText(this IGraphics graphics, string text, string fontFamilyName, int size, Color color, Point position)
        {
            var canvas = GetCanvas(graphics);
            SKFont fnt = new SKFont(SKTypeface.FromFamilyName(fontFamilyName), size);
            var paint = new SKPaint(fnt);
            paint.Color = new SKColor((uint)color.ToArgb());
            paint.TextAlign = SKTextAlign.Left;
            paint.TextEncoding = SKTextEncoding.Utf16;
            int lineSpacing = size + 2;
            SKPoint currentPosition = new SKPoint(position.X, position.Y + size); // drawing begins to the right and above the given point.
            var texts = text.Split(new char[]
            {
                '\r', '\n'
            }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

            foreach (var t in texts)
            {
                canvas.DrawText(t, currentPosition, paint);
                currentPosition.Y += lineSpacing;
            }
        }

        private static SKCanvas GetCanvas(IGraphics graphics)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            if (graphics is SkiaSharpBitmap.BitmapCanvas bitmapCanvas)
            {
                return bitmapCanvas.Canvas;
            }

            throw new ArgumentException("These extension methods can only be used on SkiaSharpBitmap instances", nameof(graphics));
        }
    }
}

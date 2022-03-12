// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace Iot.Device.Ili934x
{
    public partial class Ili9341
    {
        /// <summary>
        /// Send a bitmap to the Ili9341 display.
        /// </summary>
        /// <param name="bm">The bitmap to be sent to the display controller note that only Pixel Format Format32bppArgb is supported.</param>
        public void SendBitmap(Image<Rgba32> bm)
        {
            int width = (int)ScreenWidth;
            if (width > bm.Width)
            {
                width = bm.Width;
            }

            int height = (int)ScreenHeight;
            if (height > bm.Height)
            {
                height = bm.Height;
            }

            SendBitmap(bm, new Point(0, 0), new Rectangle(0, 0, width, height));
        }

        /// <summary>
        /// Send a bitmap to the Ili9341 display specifying the starting position and destination clipping rectangle.
        /// </summary>
        /// <param name="bm">The bitmap to be sent to the display controller note that only Pixel Format Format32bppArgb is supported.</param>
        /// <param name="updateRect">A rectangle that defines where in the display the bitmap is written. Note that no scaling is done.</param>
        public void SendBitmap(Image<Rgba32> bm, Rectangle updateRect)
        {
            SendBitmap(bm, new Point(updateRect.X, updateRect.Y), updateRect);
        }

        /// <summary>
        /// Send a bitmap to the Ili9341 display specifying the starting position and destination clipping rectangle.
        /// </summary>
        /// <param name="bm">The bitmap to be sent to the display controller note that only Pixel Format Format32bppArgb is supported.</param>
        /// <param name="sourcePoint">A coordinate point in the source bitmap where copying starts from.</param>
        /// <param name="destinationRect">A rectangle that defines where in the display the bitmap is written. Note that no scaling is done.</param>
        public void SendBitmap(Image<Rgba32> bm, Point sourcePoint, Rectangle destinationRect)
        {
            if (bm is null)
            {
                throw new ArgumentNullException(nameof(bm));
            }

            if (bm.PixelFormat != PixelFormat.Format32bppArgb)
            {
                throw new ArgumentException($"Pixel format {bm.PixelFormat.ToString()} not supported.", nameof(bm));
            }

            // get the pixel data and send it to the display
            SendBitmapPixelData(GetBitmapPixelData(bm, new Rectangle(sourcePoint.X, sourcePoint.Y, destinationRect.Width, destinationRect.Height)), destinationRect);
        }

        /// <summary>
        /// Convert a bitmap into an array of pixel data suitable for sending to the display
        /// </summary>
        /// <param name="bm">The bitmap to be sent to the display controller note that only Pixel Format Format32bppArgb is supported.</param>
        /// <param name="sourceRect">A rectangle that defines where in the bitmap data is to be converted from.</param>
        public Span<byte> GetBitmapPixelData(Image<Rgba32> bm, Rectangle sourceRect)
        {
            Rgba32[] bitmapData; // array that takes the raw bytes of the bitmap
            byte[] outputBuffer; // array used to form the data to be written out to the SPI interface

            if (bm is null)
            {
                throw new ArgumentNullException(nameof(bm));
            }

            if (bm.Width < sourceRect.Left + sourceRect.Width)
            {
                throw new ArgumentException($"Pixel format {bm.PixelFormat.ToString()} not supported.", nameof(bm));
            }

            if (bm.Height < sourceRect.Top + sourceRect.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceRect), "Rectangle exceeds size of image");
            }

            // allocate the working arrays.
            bitmapData = new Rgba32[sourceRect.Width * sourceRect.Height];
            outputBuffer = new byte[sourceRect.Width * sourceRect.Height * 2];

            // get the raw pixel data for the bitmap
            for (int i = 0; i < sourceRect.Height; i++)
            {
                var sourceLine = bm.GetPixelRowSpan(sourceRect.Top + i);
                if (sourceRect.Width == sourceLine.Length)
                {
                    sourceLine.CopyTo(new Span<Rgba32>(bitmapData).Slice(i * sourceRect.Width));
                }
                else
                {
                    // We need to copy only part of the line
                    sourceLine.Slice(sourceRect.Left, sourceRect.Width).CopyTo(new Span<Rgba32>(bitmapData).Slice(i * sourceRect.Width));
                }
            }

            // iterate over the source bitmap converting each pixel in the raw data
            // to a format suitable for sending to the display
            for (int i = 0; i < bitmapData.Length; i++)
            {
                    (outputBuffer[i * 2 + 0], outputBuffer[i * 2 + 1]) = Color565(bitmapData[i]);
            }

            return (outputBuffer);
        }

        /// <summary>
        /// Send an array of pixel data to the display.
        /// </summary>
        /// <param name="pixelData">The data to be sent to the display.</param>
        /// <param name="destinationRect">A rectangle that defines where in the display the data is to be written.</param>
        public void SendBitmapPixelData(Span<byte> pixelData, Rectangle destinationRect)
        {
            SetWindow(destinationRect.X, destinationRect.Y, (destinationRect.Right - 1), (destinationRect.Bottom - 1));   // specifiy a location for the rows and columns on the display where the data is to be written
            SendData(pixelData);
        }
    }
}

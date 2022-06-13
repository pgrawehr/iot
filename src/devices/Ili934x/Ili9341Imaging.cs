// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Iot.Device.Graphics;
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
        public void DrawBitmap(Image<Rgba32> bm)
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

            DrawBitmap(bm, new Point(0, 0), new Rectangle(0, 0, width, height));
        }

        /// <summary>
        /// Send a bitmap to the Ili9341 display specifying the starting position and destination clipping rectangle.
        /// </summary>
        /// <param name="bm">The bitmap to be sent to the display controller note that only Pixel Format Format32bppArgb is supported.</param>
        /// <param name="updateRect">A rectangle that defines where in the display the bitmap is written. Note that no scaling is done.</param>
        public void DrawBitmap(Image<Rgba32> bm, Rectangle updateRect)
        {
            DrawBitmap(bm, new Point(updateRect.X, updateRect.Y), updateRect);
        }

        /// <summary>
        /// Send a bitmap to the Ili9341 display specifying the starting position and destination clipping rectangle.
        /// </summary>
        /// <param name="bm">The bitmap to be sent to the display controller note that only Pixel Format Format32bppArgb is supported.</param>
        /// <param name="sourcePoint">A coordinate point in the source bitmap where copying starts from.</param>
        /// <param name="destinationRect">A rectangle that defines where in the display the bitmap is written. Note that no scaling is done.</param>
        public void DrawBitmap(Image<Rgba32> bm, Point sourcePoint, Rectangle destinationRect)
        {
            if (bm is null)
            {
                throw new ArgumentNullException(nameof(bm));
            }

            FillBackBufferFromImage(bm, sourcePoint, destinationRect);
        }

        private void FillBackBufferFromImage(Image<Rgba32> image, Point sourcePoint, Rectangle destinationRect)
        {
            if (image is null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            Converters.AdjustImageDestination(image, ref sourcePoint, ref destinationRect);

            _screenBuffer.ProcessPixelRows(x =>
            {
                for (int y = destinationRect.Y; y < destinationRect.Height + destinationRect.Y; y++)
                {
                    var row = x.GetRowSpan(y);
                    for (int i = destinationRect.X; i < destinationRect.Width + destinationRect.X; i++)
                    {
                        int xSource = sourcePoint.X + i - destinationRect.X;
                        int ySource = sourcePoint.Y + y - destinationRect.Y;
                        row[i] = Rgb565.FromRgba32(image[xSource, ySource]);
                    }
                }
            });
        }

        /// <summary>
        /// Updates the display with the current screen buffer.
        /// <param name="forceFull">Forces a full update, otherwise only changed screen contents are updated</param>
        /// </summary>
        public void SendFrame(bool forceFull = false)
        {
            if (!_screenBuffer.DangerousTryGetSinglePixelMemory(out var memory))
            {
                throw new NotSupportedException("Unable to retrieve image bitmap for drawing");
            }

            if (forceFull)
            {
                SetWindow(0, 0, ScreenWidth, ScreenHeight);
                SendSPI(MemoryMarshal.Cast<Rgb565, byte>(memory.Span));
            }
            else
            {
                int topRow = 0;
                int bottomRow = _screenBuffer.Height;
                for (int y = 0; y < _screenBuffer.Height; y++)
                {
                    for (int x = 0; x < _screenBuffer.Width; x++)
                    {
                        if (!Rgb565.AlmostEqual(_screenBuffer[x, y], _previousBuffer[x, y], 2))
                        {
                            topRow = y;
                            goto reverse;
                        }
                    }
                }

                // if we get here, there were no screen changes
                return;

                reverse:

                for (int y = _screenBuffer.Height - 1; y >= topRow; y--)
                {
                    for (int x = 0; x < _screenBuffer.Width; x++)
                    {
                        if (!Rgb565.AlmostEqual(_screenBuffer[x, y], _previousBuffer[x, y], 2))
                        {
                            bottomRow = y;
                            goto end;
                        }
                    }
                }

                end:

                SetWindow(0, topRow, _screenBuffer.Width, bottomRow);
                // Send the given number of rows (+1, because including the end row)
                var partialSpan = MemoryMarshal.Cast<Rgb565, byte>(memory.Span.Slice(topRow * _screenBuffer.Width, (bottomRow - topRow + 1) * _screenBuffer.Width));
                SendSPI(partialSpan);
            }

            if (!_previousBuffer.DangerousTryGetSinglePixelMemory(out var previousMemoryBuffer))
            {
                throw new NotSupportedException("Unable to retrieve background cache data for update");
            }

            _screenBuffer.CopyPixelDataTo(previousMemoryBuffer.Span);
        }

        /// <summary>
        /// Convert a bitmap into an array of pixel data suitable for sending to the display
        /// </summary>
        /// <param name="bm">The bitmap to be sent to the display controller note that only Pixel Format Format32bppArgb is supported.</param>
        /// <param name="sourceRect">A rectangle that defines where in the bitmap data is to be converted from.</param>
        [Obsolete("Use conversion to Rgb565 instead, or use FillBackBufferImage")]
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
                throw new ArgumentOutOfRangeException(nameof(sourceRect), "Rectangle exceeds width of image");
            }

            if (bm.Height < sourceRect.Top + sourceRect.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceRect), "Rectangle exceeds height of image");
            }

            // allocate the working arrays.
            bitmapData = new Rgba32[sourceRect.Width * sourceRect.Height];
            outputBuffer = new byte[sourceRect.Width * sourceRect.Height * 2];

            // get the raw pixel data for the bitmap
            bm.ProcessPixelRows(x =>
            {
                for (int i = 0; i < sourceRect.Height; i++)
                {
                    var sourceLine = x.GetRowSpan(sourceRect.Top + i);
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
            });

            // iterate over the source bitmap converting each pixel in the raw data
            // to a format suitable for sending to the display
            for (int i = 0; i < bitmapData.Length; i++)
            {
                (outputBuffer[i * 2 + 0], outputBuffer[i * 2 + 1]) = Color565(bitmapData[i]);
            }

            return (outputBuffer);
        }

        /// <summary>
        /// Convert a color structure to a byte tuple representing the colour in 565 format.
        /// </summary>
        /// <param name="color">The color to be converted.</param>
        /// <returns>
        /// This method returns the low byte and the high byte of the 16bit value representing RGB565 or BGR565 value
        ///
        /// byte    11111111 00000000
        /// bit     76543210 76543210
        ///
        /// For ColorSequence.RGB
        ///         RRRRRGGG GGGBBBBB
        ///         43210543 21043210
        ///
        /// For ColorSequence.BGR
        ///         BBBBBGGG GGGRRRRR
        ///         43210543 21043210
        /// </returns>
        [Obsolete]
        private (byte Low, byte High) Color565(Rgba32 color)
        {
            // get the top 5 MSB of the blue or red value
            UInt16 retval = (UInt16)(color.R >> 3);
            // shift right to make room for the green Value
            retval <<= 6;
            // combine with the 6 MSB if the green value
            retval |= (UInt16)(color.G >> 2);
            // shift right to make room for the red or blue Value
            retval <<= 5;
            // combine with the 6 MSB if the red or blue value
            retval |= (UInt16)(color.B >> 3);
            return ((byte)(retval >> 8), (byte)(retval & 0xFF));
        }

        /// <summary>
        /// Send an array of pixel data to the display.
        /// </summary>
        /// <param name="pixelData">The data to be sent to the display.</param>
        /// <param name="destinationRect">A rectangle that defines where in the display the data is to be written.</param>
        /// <remarks>This directly sends the data, circumventing the screen buffer</remarks>
        public void SendBitmapPixelData(Span<byte> pixelData, Rectangle destinationRect)
        {
            SetWindow(destinationRect.X, destinationRect.Y, (destinationRect.Right - 1), (destinationRect.Bottom - 1));   // specifiy a location for the rows and columns on the display where the data is to be written
            SendData(pixelData);
        }
    }
}

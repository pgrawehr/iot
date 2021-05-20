// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Iot.Device.Common;
using Iot.Device.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnitsNet;
using Xunit;

namespace Iot.Device.Common.Tests
{
    public class VideoDeviceTests
    {
        [Fact]
        public void RgbToBitmap()
        {
            Color[] colors = new Color[256 * 64];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.DarkRed;
            }

            colors[0] = Color.Blue;

            var image = VideoDevice.RgbToBitmap<Rgb24>((256, 64), colors);

            Assert.Equal(Color.Blue.ToPixel<Rgb24>(), image[0, 0]);
            for (int j = 1; j < image.Height; j++)
            {
                for (int i = 0; i < image.Width; i++)
                {
                    var pixel = image[i, j];
                    Assert.Equal(Color.DarkRed.ToPixel<Rgb24>(), pixel);
                }
            }
        }
    }
}

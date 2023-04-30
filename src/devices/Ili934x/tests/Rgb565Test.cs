// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Ili934x;
using Xunit;

namespace Iot.Device.Ili934x.Tests
{
    public class Rgb565Test
    {
        [Fact]
        public void Convert()
        {
            Rgb565 a = new Rgb565();
            Assert.Equal(Color.FromArgb(0, 0, 0), a.ToColor());

            Rgb565 b = Rgb565.FromRgba32(Color.White);
            Assert.Equal(Color.FromArgb(255, 255, 255), b.ToColor());

            b = Rgb565.FromRgba32(Color.Red);
            Assert.Equal(Color.FromArgb(255, 0, 0), b.ToColor());

            Rgb565 d = new Rgb565(0xf800);
            Assert.Equal(0xf800, d.PackedValue);
        }

        [Fact]
        public void Init()
        {
            Rgb565 c = new Rgb565(101, 52, 10);
            Assert.Equal(0x9AC, c.PackedValue);

            c = new Rgb565(255, 255, 255);
            Assert.Equal(0xFFFF, c.PackedValue);

            Rgb565 d = new Rgb565(0xff, 0, 0);
            Assert.Equal(0x001f, d.PackedValue);
            Assert.Equal(255, d.R);

            Rgb565 e = new Rgb565(0, 0xff, 0);
            Assert.Equal(0x07e0, e.PackedValue);
            Assert.Equal(255, e.G);

            Rgb565 f = new Rgb565(0, 0, 0xff);
            Assert.Equal(0xf800, f.PackedValue);
            Assert.Equal(255, f.B);
        }

        [Fact]
        public void EqualIsEqual()
        {
            Rgb565 a = new Rgb565(10, 52, 101);
            Rgb565 b = new Rgb565(a.PackedValue);
            Rgb565 c = new Rgb565(0x2345);
            Assert.True(a == b);
            Assert.True(a.Equals(b));
            Assert.True(a != c);
            Assert.True(!a.Equals(c));
        }

        [Fact]
        public void AlmostEqual()
        {
            Rgb565 a = new Rgb565(10, 52, 101);
            Rgb565 b = new Rgb565(12, 53, 102);
            // The delta value is in visible bits, so these are anyway equal
            Assert.True(Rgb565.AlmostEqual(a, b, 0));

            a = new Rgb565(0, 52, 101);
            b = new Rgb565(7, 53, 102);
            Assert.True(Rgb565.AlmostEqual(a, b, 1));
        }
    }
}

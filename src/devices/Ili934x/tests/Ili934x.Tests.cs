// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Drawing;
using Ili934x.Tests;
using Iot.Device.Graphics;
using Moq;
using Xunit;

namespace Iot.Device.Ili934x.Tests
{
    public class Ili9342Test : IDisposable
    {
        private readonly DummySpiDriver m_spiMock;
        private readonly GpioController m_gpioController;
        private readonly Mock<MockableGpioDriver> m_gpioDriverMock;
        private readonly Mock<IImageFactory> m_imageFactoryMock;

        private Ili9342? m_testee;

        public Ili9342Test()
        {
            m_spiMock = new DummySpiDriver();
            m_gpioDriverMock = new Mock<MockableGpioDriver>(MockBehavior.Loose);
            m_imageFactoryMock = new Mock<IImageFactory>(MockBehavior.Strict);
            ImageFactoryRegistry.RegisterImageFactory(m_imageFactoryMock.Object);
            m_gpioDriverMock.CallBase = true;
            m_gpioController = new GpioController(PinNumberingScheme.Logical, m_gpioDriverMock.Object);
        }

        public void Dispose()
        {
            m_testee?.Dispose();
            m_testee = null!;
        }

        [Fact]
        public void Init()
        {
            m_gpioDriverMock.Setup(x => x.OpenPinEx(15));
            m_gpioDriverMock.Setup(x => x.IsPinModeSupportedEx(It.Is<int>(y => y == 15 || y == 2 || y == 3), PinMode.Output)).Returns(true);
            m_gpioDriverMock.Setup(x => x.OpenPinEx(2));
            m_gpioDriverMock.Setup(x => x.OpenPinEx(3));
            m_gpioDriverMock.Setup(x => x.WriteEx(15, It.IsAny<PinValue>()));
            m_testee = new Ili9342(m_spiMock, 15, 2, 3, 4096, m_gpioController, false);

            Assert.NotEmpty(m_spiMock.Data);
        }

        [Fact]
        public void Size()
        {
            Init();
            Assert.Equal(320, m_testee!.ScreenWidth);
            Assert.Equal(240, m_testee.ScreenHeight);
        }

        [Fact]
        public void SendImage()
        {
            Init();

            m_imageFactoryMock.Setup(x => x.CreateBitmap(It.IsAny<int>(), It.IsAny<int>(), PixelFormat.Format32bppArgb)).Returns(
                new Func<int, int, PixelFormat, BitmapImage>((int w, int h, PixelFormat pf) =>
                {
                    var m = new Mock<BitmapImage>(MockBehavior.Loose, w, h, w * 4, pf);
                    m.CallBase = true;
                    return m.Object;
                }));
            using var bmp = m_testee!.CreateBackBuffer();

            Assert.Equal(320, bmp.Width);
            Assert.Equal(240, bmp.Height);

            bmp.SetPixel(0, 0, Color.White);
            m_spiMock.Data.Clear();
            m_testee.DrawBitmap(bmp);
            m_testee.SendFrame(true);

            // 11 bytes setup + 2 bytes per pixel (this is raw SPI data, not including any possible Arduino headers)
            Assert.Equal(11 + (320 * 240 * 2), m_spiMock.Data.Count);
        }
    }
}

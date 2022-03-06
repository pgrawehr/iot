// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Iot.Device.Arduino;
using Iot.Device.Common;
using Iot.Device.Ft4222;
using Iot.Device.Graphics;
using Iot.Device.Ili934x;
using SixLabors.ImageSharp.PixelFormats;

Console.WriteLine("Are you using Ft4222? Type 'yes' and press ENTER if so, anything else will be treated as no.");
bool isFt4222 = Console.ReadLine() == "yes";
bool isArduino = true;

////if (!isFt4222)
////{
////    Console.WriteLine("Are you using an Arduino/Firmata? Type 'yes' and press ENTER if so.");
////    isArduino = Console.ReadLine() == "yes";
////}

int pinDC = isFt4222 ? 1 : 23;
int pinReset = isFt4222 ? 0 : 24;
int pinLed = isFt4222 ? 2 : -1;

if (isArduino)
{
    // Pin mappings for the display in an M5Core2/M5Though
    pinDC = 15;
    pinReset = -1;
    pinLed = -1;
}

LogDispatcher.LoggerFactory = new SimpleConsoleLoggerFactory();
using Bitmap dotnetBM = new(240, 320);
using Graphics g = Graphics.FromImage(dotnetBM);
SpiDevice displaySPI;
ArduinoBoard? board = null;
GpioController gpio;
int spiBufferSize = 4096;
if (isFt4222)
{
    gpio = GetGpioControllerFromFt4222();
    displaySPI = GetSpiFromFt4222();
}
else if (isArduino)
{
    if (!ArduinoBoard.TryConnectToNetworkedBoard(IPAddress.Parse("192.168.1.27"), 27016, out board))
    {
        throw new IOException("Couldn't connect to board");
    }

    gpio = board.CreateGpioController();
    displaySPI = board.CreateSpiDevice(new SpiConnectionSettings(0, 5) { ClockFrequency = 50_000_000 });
    spiBufferSize = 200; // requires extended Firmata firmware, default is 25
}
else
{
    gpio = new GpioController();
    displaySPI = GetSpiFromDefault();
}

using Ili9342 ili9341 = new(displaySPI, pinDC, pinReset, backlightPin: pinLed, gpioController: gpio, spiBufferSize: spiBufferSize, shouldDispose: false);

while (!Console.KeyAvailable)
{
    foreach (string filepath in Directory.GetFiles(@"images", "*.png").OrderBy(f => f))
    {
        Console.WriteLine($"Drawing {filepath}");
        using Bitmap bm = (Bitmap)Bitmap.FromFile(filepath);
        g.Clear(Color.Black);
        g.DrawImage(bm, 0, 0, bm.Width, bm.Height);
        var converted = Converters.ToImage(dotnetBM);
        ili9341.SendBitmap(converted);
        //// Task.Delay(1000).Wait();
    }

    Console.WriteLine("FillRect(Color.Red, 120, 160, 60, 80)");
    ili9341.FillRect(new Rgba32(255, 0, 0), 120, 160, 60, 80);
    //// Task.Delay(1000).Wait();

    Console.WriteLine("FillRect(Color.Blue, 0, 0, 320, 240)");
    ili9341.FillRect(new Rgba32(0, 0, 255), 0, 0, 320, 240);
    //// Task.Delay(1000).Wait();

    Console.WriteLine("ClearScreen()");
    ili9341.ClearScreen();
    //// Task.Delay(1000).Wait();

    Console.WriteLine("FillRect(Color.Green, 0, 0, 120, 160)");
    ili9341.FillRect(new Rgba32(0, 255, 0), 0, 0, 120, 160);
    //// Task.Delay(1000).Wait();
}

Console.ReadKey(true);

int left = 0;
int top = 0;
bool abort = false;
ScreenCapture capture = new ScreenCapture();
while (!abort)
{
    var bmp = capture.GetScreenContents();
    ili9341.SendBitmap(bmp, new Point(left, top), new Rectangle(0, 0, (int)ili9341.ScreenWidth, (int)ili9341.ScreenHeight));
    if (Console.KeyAvailable)
    {
        switch (Console.ReadKey(true).Key)
        {
            case ConsoleKey.Escape:
                abort = true;
                break;
            case ConsoleKey.RightArrow:
                left += 10;
                break;
            case ConsoleKey.DownArrow:
                top += 10;
                break;
            case ConsoleKey.LeftArrow:
                left -= 10;
                break;
            case ConsoleKey.UpArrow:
                top -= 10;
                break;
        }

        if (left < 0)
        {
            left = 0;
        }

        if (left > bmp.Width - ili9341.ScreenWidth)
        {
            left = bmp.Width - (int)ili9341.ScreenWidth;
        }

        if (top < 0)
        {
            top = 0;
        }

        if (top > bmp.Height - ili9341.ScreenHeight)
        {
            top = bmp.Height - (int)ili9341.ScreenHeight;
        }
    }
}

capture.Dispose();

ili9341.Dispose();

board?.Dispose();

GpioController GetGpioControllerFromFt4222()
{
    return new GpioController(PinNumberingScheme.Logical, new Ft4222Gpio());
}

SpiDevice GetSpiFromFt4222()
{
    return new Ft4222Spi(new SpiConnectionSettings(0, 1) { ClockFrequency = Ili9341.DefaultSpiClockFrequency, Mode = Ili9341.DefaultSpiMode });
}

SpiDevice GetSpiFromDefault()
{
    return SpiDevice.Create(new SpiConnectionSettings(0, 0) { ClockFrequency = Ili9341.DefaultSpiClockFrequency, Mode = Ili9341.DefaultSpiMode });
}

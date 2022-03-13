// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Iot.Device.Arduino;
using Iot.Device.Axp192;
using Iot.Device.Common;
using Iot.Device.Ft4222;
using Iot.Device.Graphics;
using Iot.Device.Ili934x;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using UnitsNet;

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
SpiDevice displaySPI;
ArduinoBoard? board = null;
GpioController gpio;
int spiBufferSize = 4096;
M5ToughPowerControl? powerControl = null;
Chsc6440? touch = null;

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
    powerControl = new M5ToughPowerControl(board);
    powerControl.EnableSpeaker = false; // With my current firmware, it's used instead of the status led. Noisy!

    touch = new Chsc6440(board.CreateI2cDevice(new I2cConnectionSettings(0, Chsc6440.DefaultI2cAddress)), 39, board.CreateGpioController(), false);
    touch.UpdateInterval = TimeSpan.FromMilliseconds(100);
    touch.EnableEvents();
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
        using var bm = Image<Rgba32>.Load<Rgba32>(filepath);
        ili9341.SendBitmap(bm);
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

    if (powerControl != null)
    {
        var pc = powerControl.GetPowerControlData();
        using Image<Rgba32> bmp = ili9341.CreateBackBuffer();
        FontFamily family = SystemFonts.Get("Arial");
        Font font = new Font(family, 20);
        bmp.Mutate(x => x.DrawText(pc.ToString(), font, SixLabors.ImageSharp.Color.Blue, new PointF(20, 10)));
        ili9341.SendBitmap(bmp);
    }
}

Console.ReadKey(true);

int left = 0;
int top = 0;
float scale = 1.0f;
bool abort = false;
Point? lastTouchPoint = null;
ScreenCapture capture = new ScreenCapture();
ElectricPotential backLight = ElectricPotential.FromMillivolts(3000);
if (touch != null)
{
    touch.Touched += (o, point) =>
    {
        lastTouchPoint = point;
        Console.WriteLine($"Touched screen at {point}");
    };
}

while (!abort)
{
    bool backLightChanged = false;
    Stopwatch sw = Stopwatch.StartNew();
    if (Console.KeyAvailable)
    {
        var key = Console.ReadKey(true).Key;
        switch (key)
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
            case ConsoleKey.Add:
                scale *= 1.1f;
                break;
            case ConsoleKey.Subtract:
                scale /= 1.1f;
                break;
            case ConsoleKey.Insert:
                backLight = backLight + ElectricPotential.FromMillivolts(200);
                backLightChanged = true;
                break;
            case ConsoleKey.Delete:
                backLight = backLight - ElectricPotential.FromMillivolts(200);
                backLightChanged = true;
                break;
        }

    }

    if (powerControl != null && backLightChanged)
    {
        powerControl.SetLcdVoltage(backLight);
    }

    var bmp = capture.GetScreenContents();
    if (bmp != null)
    {
        bmp.Mutate(x => x.Resize((int)(bmp.Width * scale), (int)(bmp.Height * scale)));
        var pt = new Point(left, top);
        var rect = new Rectangle(0, 0, ili9341.ScreenWidth, ili9341.ScreenHeight);
        Converters.AdjustImageDestination(bmp, ref pt, ref rect);
        left = pt.X;
        top = pt.Y;
        if (lastTouchPoint != null)
        {
            var touchPos = lastTouchPoint.Value;
            bmp.Mutate(x => x.Draw(Color.Red, 3.0f, new RectangleF(touchPos.X - 1, touchPos.Y - 1, 3, 3)));
            lastTouchPoint = null;
        }

        ili9341.SendBitmap(bmp, pt, rect);
        bmp.Dispose();
    }

    ////if (touch != null)
    ////{
    ////    if (touch.IsPressed())
    ////    {
    ////        Console.WriteLine("Oh, you're touching me");
    ////    }
    ////    else
    ////    {
    ////        Console.WriteLine("Touch me!");
    ////    }

    ////    var pt = touch.GetPrimaryTouchPoint();
    ////    if (pt != null)
    ////    {
    ////        Console.WriteLine($"Touch point: {pt.Value.X}/{pt.Value.Y}");
    ////    }
    ////}

    Console.WriteLine($"Last frame took {sw.Elapsed.TotalMilliseconds}ms ({1.0 / sw.Elapsed.TotalSeconds} FPS)");
}

touch?.Dispose();
capture.Dispose();

ili9341.Dispose();

board?.Dispose();
powerControl?.Dispose();

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

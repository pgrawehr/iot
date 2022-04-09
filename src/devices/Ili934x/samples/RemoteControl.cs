// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Axp192;
using Iot.Device.Graphics;
using Iot.Device.Ili934x;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UnitsNet;

namespace Iot.Device.Ili934x.Samples
{
    internal class RemoteControl
    {
        // Note: Owner of these is the outer class
        private readonly Chsc6440? _touch;
        private readonly Ili9342 _ili9341;
        private readonly M5ToughPowerControl? _powerControl;

        private bool _menuMode;

        public RemoteControl(Chsc6440? touch, Ili9342 ili9341, M5ToughPowerControl? powerControl)
        {
            _touch = touch;
            _ili9341 = ili9341;
            _powerControl = powerControl;
            _menuMode = false;
        }

        public void DisplayFeatures()
        {
            DemoMode();

            float left = 0;
            float top = 0;
            float scale = 1.0f;
            bool abort = false;
            Point? lastTouchPoint = null;
            using ScreenCapture capture = new ScreenCapture();
            ElectricPotential backLight = ElectricPotential.FromMillivolts(3000);
            Point dragBegin = Point.Empty;

            if (_touch != null)
            {
                _touch.Touched += (o, point) =>
                {
                    lastTouchPoint = point;
                    Console.WriteLine($"Touched screen at {point}");
                };

                _touch.Dragging += (o, e) =>
                {
                    var (xdiff, ydiff) = (e.LastPoint.X - e.CurrentPoint.X, e.LastPoint.Y - e.CurrentPoint.Y);
                    left += xdiff * scale;
                    top += ydiff * scale;
                    Console.WriteLine($"Dragging at {e.CurrentPoint.X}/{e.CurrentPoint.Y} by {xdiff}/{ydiff}.");
                    if (e.IsDragBegin)
                    {
                        dragBegin = e.LastPoint;
                    }

                    if (dragBegin.Y < 10 && e.CurrentPoint.Y > 50)
                    {
                        Console.WriteLine("Showing menu");
                        _menuMode = true;
                    }
                };

                _touch.Zooming += (o, points, oldDiff, newDiff) =>
                {
                    float scaleChange = (float)oldDiff / newDiff;
                    if (scaleChange != 0)
                    {
                        scale = scale / scaleChange;
                    }
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

                if (_powerControl != null && backLightChanged)
                {
                    _powerControl.SetLcdVoltage(backLight);
                }

                DrawScreenContents(capture, scale, ref left, ref top, lastTouchPoint);
                lastTouchPoint = null;

                if (_menuMode)
                {
                    _ili9341.FillRect(new Rgba32(0, 0, 200), 0, 0, _ili9341.ScreenWidth, _ili9341.ScreenWidth);
                    _ili9341.FillRect(new Rgba32(0, 0, 0), 2, 2, _ili9341.ScreenWidth - 4, _ili9341.ScreenWidth - 4);
                }

                _ili9341.SendFrame();

                Console.WriteLine($"Last frame took {sw.Elapsed.TotalMilliseconds}ms ({1.0 / sw.Elapsed.TotalSeconds} FPS)");
            }
        }

        private void DrawScreenContents(ScreenCapture capture, float scale, ref float left, ref float top, Point? lastTouchPoint)
        {
            var bmp = capture.GetScreenContents();
            if (bmp != null)
            {
                _ili9341.FillRect(Color.Black, 0, 0, _ili9341.ScreenWidth, _ili9341.ScreenHeight, false);
                bmp.Mutate(x => x.Resize((int)(bmp.Width * scale), (int)(bmp.Height * scale)));
                var pt = new Point((int)left, (int)top);
                var rect = new Rectangle(0, 0, _ili9341.ScreenWidth, _ili9341.ScreenHeight);
                Converters.AdjustImageDestination(bmp, ref pt, ref rect);
                left = pt.X;
                top = pt.Y;
                if (lastTouchPoint != null)
                {
                    var touchPos = lastTouchPoint.Value;
                    bmp.Mutate(x => x.Draw(Color.Red, 3.0f, new RectangleF(touchPos.X - 1, touchPos.Y - 1, 3, 3)));
                    lastTouchPoint = null;
                }

                _ili9341.DrawBitmap(bmp, pt, rect);
                bmp.Dispose();
            }
        }

        private void DemoMode()
        {
            while (!Console.KeyAvailable && (_touch != null && !_touch.IsPressed()))
            {
                foreach (string filepath in Directory.GetFiles(@"images", "*.png").OrderBy(f => f))
                {
                    Console.WriteLine($"Drawing {filepath}");
                    using var bm = Image<Rgba32>.Load<Rgba32>(filepath);
                    _ili9341.DrawBitmap(bm);
                    _ili9341.SendFrame();
                }

                Console.WriteLine("FillRect(Color.Red, 120, 160, 60, 80)");
                _ili9341.FillRect(new Rgba32(255, 0, 0), 120, 160, 60, 80, true);

                Console.WriteLine("FillRect(Color.Blue, 0, 0, 320, 240)");
                _ili9341.FillRect(new Rgba32(0, 0, 255), 0, 0, 320, 240, true);

                Console.WriteLine("ClearScreen()");
                _ili9341.ClearScreen();

                DrawPowerStatus();
                _ili9341.SendFrame();

                Thread.Sleep(1000);
            }

            if (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }
        }

        private void DrawPowerStatus()
        {
            if (_powerControl != null)
            {
                var pc = _powerControl.GetPowerControlData();
                using Image<Rgba32> bmp = _ili9341.CreateBackBuffer();
                FontFamily family = SystemFonts.Get("Arial");
                Font font = new Font(family, 20);
                bmp.Mutate(x => x.DrawText(pc.ToString(), font, SixLabors.ImageSharp.Color.Blue, new PointF(0, 10)));
                _ili9341.DrawBitmap(bmp);
            }
        }
    }
}

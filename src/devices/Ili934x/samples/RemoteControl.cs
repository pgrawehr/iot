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
        private readonly Ili9341 _screen;
        private readonly M5ToughPowerControl? _powerControl;

        private readonly Image<Rgba32> _defaultMenuBar;
        private readonly Image<Rgba32> _leftMouseMenuBar;
        private readonly Image<Rgba32> _rightMouseMenuBar;
        private readonly Image<Rgba32> _openMenu;

        private bool _menuMode;
        private float _left;
        private float _top;
        private float _scale;
        private ElectricPotential _backLight;
        private ScreenMode _screenMode;
        private MouseButtonMode _mouseEnabled;
        private MouseClickSimulator _clickSimulator;
        private Point _lastDragBegin;

        public RemoteControl(Chsc6440? touch, Ili9342 ili9341, M5ToughPowerControl? powerControl)
        {
            _touch = touch;
            _screen = ili9341;
            _powerControl = powerControl;
            _menuMode = false;
            _left = 0;
            _top = 0;
            _scale = 1.0f;
            _screenMode = ScreenMode.Mirror;
            _backLight = ElectricPotential.FromMillivolts(3000);
            _mouseEnabled = MouseButtonMode.None;
            _clickSimulator = new MouseClickSimulator();

            _leftMouseMenuBar = Image.Load<Rgba32>("images/MenuBarLeftMouse.png");
            _rightMouseMenuBar = Image.Load<Rgba32>("images/MenuBarRightMouse.png");
            _defaultMenuBar = Image.Load<Rgba32>("images/MenuBar.png");
            _openMenu = Image.Load<Rgba32>("images/OpenMenu.png");
        }

        private void OnTouched(object o, Point point)
        {
            Console.WriteLine($"Touched screen at {point}");
            // For the coordinates here, see the MenuBar.png file
            if (_menuMode && point.Y < 100)
            {
                if (point.X > 222)
                {
                    _menuMode = false;
                    _screenMode = ScreenMode.Mirror;
                }
                else if (point.Y < 50 && point.X > 100 && point.Y < 160)
                {
                    _screenMode = ScreenMode.Battery;
                    _menuMode = false;
                }
                else if (point.Y > 50 && point.X > 100 && point.Y < 160)
                {
                    _scale /= 1.1f;
                }
                else if (point.Y > 50 && point.X > 160 && point.Y < 220)
                {
                    _scale *= 1.1f;
                }
                else if (point.X < 100)
                {
                    if (_mouseEnabled == MouseButtonMode.Left)
                    {
                        _mouseEnabled = MouseButtonMode.Right;
                    }
                    else if (_mouseEnabled == MouseButtonMode.Right)
                    {
                        _mouseEnabled = MouseButtonMode.None;
                    }
                    else
                    {
                        _mouseEnabled = MouseButtonMode.Left;
                    }

                    Console.WriteLine($"Mouse mode: {_mouseEnabled}");
                }
            }
            else
            {
                if (point.X > _screen.ScreenWidth - 30 && point.Y < 30)
                {
                    _menuMode = true;
                }

                if (_mouseEnabled != MouseButtonMode.None)
                {
                    _clickSimulator.PerformClick(ToAbsoluteScreenPosition(point), _mouseEnabled);
                }
            }
        }

        private Point ToAbsoluteScreenPosition(Point point)
        {
            return new Point((int)((point.X + _left) / _scale), (int)((point.Y + _top) / _scale));
        }

        private void OnDragging(object o, DragEventArgs e)
        {
            if (_mouseEnabled == MouseButtonMode.Left)
            {
                if (e.IsDragBegin)
                {
                    _clickSimulator.MouseDown(ToAbsoluteScreenPosition(e.CurrentPoint), MouseButtonMode.Left);
                    _lastDragBegin = e.CurrentPoint;
                }

                _clickSimulator.MouseMove(ToAbsoluteScreenPosition(e.CurrentPoint));
                if (e.IsDragEnd)
                {
                    _clickSimulator.MouseUp(ToAbsoluteScreenPosition(e.CurrentPoint), MouseButtonMode.Left);
                    _lastDragBegin = new Point(99999, 99999); // Outside
                }

                return;
            }

            var (xdiff, ydiff) = (e.LastPoint.X - e.CurrentPoint.X, e.LastPoint.Y - e.CurrentPoint.Y);
            _left += xdiff * _scale;
            _top += ydiff * _scale;
            Console.WriteLine($"Dragging at {e.CurrentPoint.X}/{e.CurrentPoint.Y} by {xdiff}/{ydiff}.");
            if (e.IsDragBegin)
            {
                _lastDragBegin = e.LastPoint;
            }
            else if (e.IsDragEnd)
            {
                _lastDragBegin = new Point(99999, 99999); // Outside}
            }
        }

        public void IncreaseBrightness()
        {
            _backLight = _backLight + ElectricPotential.FromMillivolts(200);
            if (_powerControl != null)
            {
                _powerControl.SetLcdVoltage(_backLight);
            }
        }

        public void DecreaseBrightness()
        {
            _backLight = _backLight - ElectricPotential.FromMillivolts(200);
            if (_powerControl != null)
            {
                _powerControl.SetLcdVoltage(_backLight);
            }
        }

        public void DisplayFeatures()
        {
            DemoMode();

            bool abort = false;
            using ScreenCapture capture = new ScreenCapture();
            Point dragBegin = Point.Empty;

            if (_touch != null)
            {
                _touch.Touched += OnTouched;

                _touch.Dragging += OnDragging;

                _touch.Zooming += (o, points, oldDiff, newDiff) =>
                {
                    float scaleChange = (float)oldDiff / newDiff;
                    if (scaleChange != 0)
                    {
                        _scale = _scale / scaleChange;
                    }
                };
            }

            while (!abort)
            {
                Stopwatch sw = Stopwatch.StartNew();
                KeyboardControl(ref abort);

                switch (_screenMode)
                {
                    case ScreenMode.Mirror:
                        DrawScreenContents(capture, _scale, ref _left, ref _top);
                        break;
                    case ScreenMode.Battery:
                        DrawPowerStatus();
                        break;
                    default:
                        _screen.ClearScreen();
                        break;
                }

                if (_menuMode)
                {
                    Image<Rgba32> bm = _defaultMenuBar;
                    if (_mouseEnabled == MouseButtonMode.Left)
                    {
                        bm = _leftMouseMenuBar;
                    }
                    else if (_mouseEnabled == MouseButtonMode.Right)
                    {
                        bm = _rightMouseMenuBar;
                    }

                    _screen.DrawBitmap(bm, new Point(0, 0), new Rectangle(0, 0, bm.Width, bm.Height));
                }
                else
                {
                    // Draw the "open menu here" icon over the top right of the screen.
                    _screen.DrawBitmap(_openMenu, new Point(0, 0), new Rectangle(_screen.ScreenWidth - _openMenu.Width, 0, _openMenu.Width, _openMenu.Height));
                }

                _screen.SendFrame();

                // Console.WriteLine($"Last frame took {sw.Elapsed.TotalMilliseconds}ms ({1.0 / sw.Elapsed.TotalSeconds} FPS)");
            }
        }

        private void KeyboardControl(ref bool abort)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.Escape:
                        abort = true;
                        break;
                    case ConsoleKey.RightArrow:
                        _left += 10;
                        break;
                    case ConsoleKey.DownArrow:
                        _top += 10;
                        break;
                    case ConsoleKey.LeftArrow:
                        _left -= 10;
                        break;
                    case ConsoleKey.UpArrow:
                        _top -= 10;
                        break;
                    case ConsoleKey.Add:
                        _scale *= 1.1f;
                        break;
                    case ConsoleKey.Subtract:
                        _scale /= 1.1f;
                        break;
                    case ConsoleKey.Insert:
                        IncreaseBrightness();
                        break;
                    case ConsoleKey.Delete:
                        DecreaseBrightness();
                        break;
                }
            }
        }

        private void DrawScreenContents(ScreenCapture capture, float scale, ref float left, ref float top)
        {
            var bmp = capture.GetScreenContents();
            if (bmp != null)
            {
                _screen.FillRect(Color.Black, 0, 0, _screen.ScreenWidth, _screen.ScreenHeight, false);
                bmp.Mutate(x => x.Resize((int)(bmp.Width * scale), (int)(bmp.Height * scale)));
                var pt = new Point((int)left, (int)top);
                var rect = new Rectangle(0, 0, _screen.ScreenWidth, _screen.ScreenHeight);
                Converters.AdjustImageDestination(bmp, ref pt, ref rect);
                left = pt.X;
                top = pt.Y;

                _screen.DrawBitmap(bmp, pt, rect);
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
                    _screen.DrawBitmap(bm);
                    _screen.SendFrame();
                }

                Console.WriteLine("FillRect(Color.Red, 120, 160, 60, 80)");
                _screen.FillRect(new Rgba32(255, 0, 0), 120, 160, 60, 80, true);

                Console.WriteLine("FillRect(Color.Blue, 0, 0, 320, 240)");
                _screen.FillRect(new Rgba32(0, 0, 255), 0, 0, 320, 240, true);

                Console.WriteLine("ClearScreen()");
                _screen.ClearScreen();
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
                using Image<Rgba32> bmp = _screen.CreateBackBuffer();
                FontFamily family = SystemFonts.Get("Arial");
                Font font = new Font(family, 20);
                bmp.Mutate(x => x.DrawText(pc.ToString(), font, SixLabors.ImageSharp.Color.Blue, new PointF(0, 10)));
                _screen.DrawBitmap(bmp);
            }
        }
    }
}

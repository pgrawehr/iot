// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using SixLabors.ImageSharp;
using static Interop;

namespace Iot.Device.Graphics
{
    // Code borrowed from https://gist.github.com/pioz/726474
    public partial class MouseClickSimulator
    {
        private IntPtr _display;
        private MouseButtonMode _currentButtons;

        private static XButtonEvent GetState(uint button, XButtonEvent ev1)
        {
            // The state is the bitmask of the buttons just prior to the event
            // Therefore it is 0 on a buttonDown event and non-zero on drag and up events
            ev1.state = 0;
            if (button == 1)
            {
                ev1.state = 256;
            }
            else if (button == 2)
            {
                ev1.state = 512;
            }
            else if (button == 3)
            {
                ev1.state = 1024;
            }

            return ev1;
        }

        private void PerformMouseClickLinux(Point pt, MouseButtonMode buttons, bool down, bool up)
        {
            MoveMouseTo(pt);
            if ((buttons & MouseButtonMode.Left) != MouseButtonMode.None)
            {
                PerformMouseClickLinux(1, down, up);
            }

            if ((buttons & MouseButtonMode.Right) != MouseButtonMode.None)
            {
                PerformMouseClickLinux(3, down, up);
            }

            if ((buttons & MouseButtonMode.Middle) != MouseButtonMode.None)
            {
                PerformMouseClickLinux(2, down, up);
            }

            if (down && !up)
            {
                _currentButtons = buttons;
            }

            if (up)
            {
                _currentButtons = MouseButtonMode.None;
            }
        }

        private MouseButtonMode GetMouseCoordinates(out Point pt)
        {
            XButtonEvent ev = default;
            XQueryPointer(_display, XDefaultRootWindow(_display),
                ref ev.root, ref ev.window,
                ref ev.x_root, ref ev.y_root,
                ref ev.x, ref ev.y,
                ref ev.state);

            pt = default;
            pt.X = ev.x;
            pt.Y = ev.y;

            MouseButtonMode mode = MouseButtonMode.None;

            if ((ev.state & 1) != 0)
            {
                mode |= MouseButtonMode.Left;
            }

            if ((ev.state & 2) != 0)
            {
                mode |= MouseButtonMode.Right;
            }

            if ((ev.state & 3) != 0)
            {
                mode |= MouseButtonMode.Middle;
            }

            return mode;
        }

        private void MoveMouseTo(Point pt)
        {
            MoveMouseTo(pt.X, pt.Y);
        }

        private void MoveMouseTo(int x, int y)
        {
            GetMouseCoordinates(out Point pt);
            // XWarpPointer(_display, Window.Zero, Window.Zero, 0, 0, 0, 0, -pt.X, -pt.Y);
            // XWarpPointer(_display, Window.Zero, Window.Zero, 0, 0, 0, 0, x, y);
            // XWarpPointer moves the mouse relative, therefore offset the movement with the current position
            XWarpPointer(_display, Window.Zero, Window.Zero, 0, 0, 0, 0, x - pt.X, y - pt.Y);

            XMotionEvent ev1 = default;
            ev1.type = MotionNotify;
            ev1.subwindow = XDefaultRootWindow(_display);
            while (ev1.subwindow != Window.Zero)
            {
                ev1.window = ev1.subwindow;
                XQueryPointer(_display, ev1.window,
                    ref ev1.root, ref ev1.subwindow,
                    ref ev1.x_root, ref ev1.y_root,
                    ref ev1.x, ref ev1.y,
                    ref ev1.state);
            }

            ev1.state = 0;
            ev1.is_hint = 0;
            if ((_currentButtons & MouseButtonMode.Left) != MouseButtonMode.None)
            {
                ev1.state = 256;
            }

            if ((_currentButtons & MouseButtonMode.Right) != MouseButtonMode.None)
            {
                ev1.state = 1024;
            }

            if ((_currentButtons & MouseButtonMode.Middle) != MouseButtonMode.None)
            {
                ev1.state = 512;
            }

            Console.WriteLine($"Mouse moving to {ev1.x}, {ev1.y}");
            XSendEvent(_display, ev1.window, true, PointerMotionMask | PointerMotionHintMask | ButtonMotionMask, ref ev1);
        }

        private void PerformMouseClickLinux(uint button, bool down, bool up)
        {
            // Create and setting up the event
            Interop.XButtonEvent ev1 = default;

            ev1.button = button;
            ev1.same_screen = true;
            ev1.send_event = 1;
            ev1.subwindow = ev1.window = XDefaultRootWindow(_display);
            while (ev1.subwindow != Window.Zero)
            {
                ev1.window = ev1.subwindow;
                XQueryPointer(_display, ev1.window,
                ref ev1.root, ref ev1.subwindow,
                ref ev1.x_root, ref ev1.y_root,
                ref ev1.x, ref ev1.y,
                ref ev1.state);
            }

            // Press
            if (down)
            {
                ev1.type = ButtonPress;
                GetState(0, ev1);
                Console.WriteLine($"Mouse is at position {ev1.x}, {ev1.y} of window {ev1.window}");
                if (XSendEvent(_display, ev1.window /* PointerWindow */, true, 0, ref ev1) == 0)
                {
                    throw new InvalidOperationException("Error sending mouse press event");
                }

                Console.WriteLine("Press event sent");
                XFlush(_display);
                Thread.Sleep(10);
            }

            if (up)
            {
                // Release
                ev1.type = ButtonRelease;
                ev1 = GetState(button, ev1);

                if (XSendEvent(_display, ev1.window, true, 0, ref ev1) == 0)
                {
                    throw new InvalidOperationException("Error sending mouse release event");
                }

                Console.WriteLine("Press release event sent");
                XFlush(_display);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Interop;

namespace Iot.Device.Graphics
{
    // Code borrowed from https://gist.github.com/pioz/726474
    public partial class MouseClickSimulator
    {
        private IntPtr _display;

        private MouseButtonMode GetButtonEvent(Window w)
        {
            XButtonEvent ev = new XButtonEvent();
            XWindowEvent(_display, w, -1, ref ev);
            if (ev.button != 0)
            {
                Console.WriteLine("Button was pressed");
                return MouseButtonMode.Left;
            }

            return MouseButtonMode.None;
        }

        private Window CreateWindow(int x, int y, int width, int height)
        {
            return XCreateSimpleWindow(_display, XDefaultRootWindow(_display), 0, 0, 100, 200, 2, 0x0FEEDDCC, 0x0FAA0020);
        }

        private void DoSomeWindowing()
        {
            Window w = CreateWindow(20, 30, 100, 200);
            XMapWindow(_display, w);
            XSelectInput(_display, w, ButtonPressMask);
            while (w != Window.Zero)
            {
                XButtonEvent ev = default;
                XWindowEvent(_display, w, ButtonPressMask, ref ev);
                if (ev.type == 4)
                {
                    Console.WriteLine($"Clicked at {ev.x}, {ev.y}");
                }
            }
        }

        private void PerformMouseClickLinux(MouseButtonMode buttons)
        {
            // Todo: Move mouse
            if ((buttons & MouseButtonMode.Left) != MouseButtonMode.None)
            {
                PerformMouseClickLinux(1);
            }

            if ((buttons & MouseButtonMode.Right) != MouseButtonMode.None)
            {
                PerformMouseClickLinux(2);
            }

            if ((buttons & MouseButtonMode.Middle) != MouseButtonMode.None)
            {
                PerformMouseClickLinux(3);
            }
        }

        private void PerformMouseClickLinux(uint button)
        {
            // Create and setting up the event
            Interop.XButtonEvent ev1 = default;

            ev1.button = button;
            ev1.same_screen = true;
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

            // Press
            ev1.type = ButtonPress;
            Console.WriteLine($"Mouse is at position {ev1.x}, {ev1.y} of window {ev1.window}");
            if (XSendEvent(_display, Window.Zero /* PointerWindow */, true, ButtonPressMask, ref ev1) == 0)
            {
                throw new InvalidOperationException("Error sending mouse press event");
            }

            Console.WriteLine("Press event sent");
            XFlush(_display);
            Thread.Sleep(1);

            // Release
            ev1.type = ButtonRelease;
            if (XSendEvent(_display, Window.Zero, true, ButtonReleaseMask, ref ev1) == 0)
            {
                throw new InvalidOperationException("Error sending mouse release event");
            }

            Console.WriteLine("Press release event sent");
            XFlush(_display);
        }
    }
}

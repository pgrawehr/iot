// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace Iot.Device.Graphics
{
    public partial class MouseClickSimulator
    {
        private static void PerformClickWindows(Point pt, MouseButtonMode buttons)
        {
            var mpt = new Interop.MousePoint(pt.X, pt.Y);
            Interop.SetCursorPosition(mpt);
            if ((buttons & MouseButtonMode.Left) != MouseButtonMode.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.LeftDown | Interop.MouseEventFlags.Absolute);
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.LeftUp | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButtonMode.Right) != MouseButtonMode.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.RightDown | Interop.MouseEventFlags.Absolute);
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.RightUp | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButtonMode.Middle) != MouseButtonMode.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.MiddleDown | Interop.MouseEventFlags.Absolute);
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.MiddleUp | Interop.MouseEventFlags.Absolute);
            }
        }

        private static void MouseDownWindows(Point pt, MouseButtonMode buttons)
        {
            var mpt = new Interop.MousePoint(pt.X, pt.Y);
            Interop.SetCursorPosition(mpt);
            if ((buttons & MouseButtonMode.Left) != MouseButtonMode.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.LeftDown | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButtonMode.Right) != MouseButtonMode.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.RightDown | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButtonMode.Middle) != MouseButtonMode.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.MiddleDown | Interop.MouseEventFlags.Absolute);
            }
        }

        private static void MouseUpWindows(Point pt, MouseButtonMode buttons)
        {
            var mpt = new Interop.MousePoint(pt.X, pt.Y);
            Interop.SetCursorPosition(mpt);
            if ((buttons & MouseButtonMode.Left) != MouseButtonMode.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.LeftUp | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButtonMode.Right) != MouseButtonMode.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.RightUp | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButtonMode.Middle) != MouseButtonMode.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.MiddleUp | Interop.MouseEventFlags.Absolute);
            }
        }

        private static void MouseMoveWindows(Point pt)
        {
            var mpt = new Interop.MousePoint(pt.X, pt.Y);
            Interop.SetCursorPosition(mpt);
        }
    }
}

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
        private static void PerformClickWindows(Point pt, MouseButton buttons)
        {
            var mpt = new Interop.MousePoint(pt.X, pt.Y);
            Interop.SetCursorPosition(mpt);
            if ((buttons & MouseButton.Left) != MouseButton.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.LeftDown | Interop.MouseEventFlags.Absolute);
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.LeftUp | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButton.Right) != MouseButton.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.RightDown | Interop.MouseEventFlags.Absolute);
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.RightUp | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButton.Middle) != MouseButton.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.MiddleDown | Interop.MouseEventFlags.Absolute);
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.MiddleUp | Interop.MouseEventFlags.Absolute);
            }
        }

        private static void MouseDownWindows(Point pt, MouseButton buttons)
        {
            var mpt = new Interop.MousePoint(pt.X, pt.Y);
            Interop.SetCursorPosition(mpt);
            if ((buttons & MouseButton.Left) != MouseButton.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.LeftDown | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButton.Right) != MouseButton.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.RightDown | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButton.Middle) != MouseButton.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.MiddleDown | Interop.MouseEventFlags.Absolute);
            }
        }

        private static void MouseUpWindows(Point pt, MouseButton buttons)
        {
            var mpt = new Interop.MousePoint(pt.X, pt.Y);
            Interop.SetCursorPosition(mpt);
            if ((buttons & MouseButton.Left) != MouseButton.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.LeftUp | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButton.Right) != MouseButton.None)
            {
                Interop.MouseEvent(mpt, Interop.MouseEventFlags.RightUp | Interop.MouseEventFlags.Absolute);
            }

            if ((buttons & MouseButton.Middle) != MouseButton.None)
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

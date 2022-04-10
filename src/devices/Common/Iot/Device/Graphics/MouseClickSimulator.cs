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
    /// <summary>
    /// This class provides an operating-system independent way of simulating mouse clicks
    /// </summary>
    public class MouseClickSimulator
    {
        /// <summary>
        /// Creates a new instance of <see cref="MouseClickSimulator"/>
        /// </summary>
        public MouseClickSimulator()
        {
        }

        /// <summary>
        /// Simulates a mouse click at the given position
        /// </summary>
        /// <param name="pt">Point, in screen coordinates</param>
        /// <param name="buttons">The button(s) to click</param>
        public void PerformClick(Point pt, MouseButtonMode buttons)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
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

                return;
            }

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Release the given button
        /// </summary>
        /// <param name="pt">Current mouse position</param>
        /// <param name="buttons">Buttons to press</param>
        public void MouseDown(Point pt, MouseButtonMode buttons)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
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

                return;
            }

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Release the given button
        /// </summary>
        /// <param name="pt">Current mouse position</param>
        /// <param name="buttons">Buttons to release</param>
        public void MouseUp(Point pt, MouseButtonMode buttons)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
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

                return;
            }

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Moves the cursor to the given absolute position. Whether this results in a drag operation or just a mouse move
        /// will depend on whether any button is currently pressed.
        /// </summary>
        /// <param name="pt">New screen position of the cursor</param>
        public void MouseMove(Point pt)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var mpt = new Interop.MousePoint(pt.X, pt.Y);
                Interop.SetCursorPosition(mpt);
                return;
            }

            throw new PlatformNotSupportedException();
        }
    }
}

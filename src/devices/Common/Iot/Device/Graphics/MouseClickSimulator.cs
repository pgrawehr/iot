// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using static Interop;

namespace Iot.Device.Graphics
{
    /// <summary>
    /// This class provides an operating-system independent way of simulating mouse clicks
    /// </summary>
    public partial class MouseClickSimulator
    {
        /// <summary>
        /// Creates a new instance of <see cref="MouseClickSimulator"/>
        /// </summary>
        public MouseClickSimulator()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                unsafe
                {
                    _display = XOpenDisplay(null);
                    if (_display == IntPtr.Zero)
                    {
                        throw new NotSupportedException("Unable to open display - XOpenDisplay failed");
                    }
                }
            }
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
                PerformClickWindows(pt, buttons);
                return;
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // DoSomeWindowing();
                PerformMouseClickLinux(pt, buttons, true, true);
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
                MouseDownWindows(pt, buttons);

                return;
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // DoSomeWindowing();
                PerformMouseClickLinux(pt, buttons, true, false);
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
                MouseUpWindows(pt, buttons);

                return;
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // DoSomeWindowing();
                PerformMouseClickLinux(pt, buttons, false, true);
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
                MouseMoveWindows(pt);
                return;
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // DoSomeWindowing();
                MoveMouseTo(pt);
                return;
            }

            throw new PlatformNotSupportedException();
        }
    }
}

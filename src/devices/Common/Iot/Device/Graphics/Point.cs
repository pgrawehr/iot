// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Iot.Device.Graphics
{
    /// <summary>
    /// A 2D Point
    /// </summary>
    public readonly struct Point
    {
        /// <summary>
        /// Constructs a point
        /// </summary>
        /// <param name="x">X coordinate (Normally to the right in an image)</param>
        /// <param name="y">Y coordinate (Normally down within an image)</param>
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// X coordinate (left to right)
        /// </summary>
        public int X
        {
            get;
            init;
        }

        /// <summary>
        /// Y coordinate (top to bottom)
        /// </summary>
        public int Y
        {
            get;
            init;
        }
    }
}

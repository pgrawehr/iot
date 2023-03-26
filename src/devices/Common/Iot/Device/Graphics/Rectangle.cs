// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Iot.Device.Graphics
{
    /// <summary>
    /// A rectangular area on screen
    /// </summary>
    public record Rectangle(int X, int Y, int Width, int Height)
    {
        /// <summary>
        /// An empty rectangle
        /// </summary>
        public Rectangle()
        : this(0, 0, 0, 0)
        {
        }
    }
}

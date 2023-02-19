// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Graphics
{
    /// <summary>
    /// A simple container for the size of a graphic element or window
    /// </summary>
    public readonly struct Size
    {
        /// <summary>
        /// Constructs a size instance from width and height
        /// </summary>
        /// <param name="width">The width of the object</param>
        /// <param name="height">The height of the object</param>
        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// The width
        /// </summary>
        public int Width
        {
            get;
        }

        /// <summary>
        /// The height
        /// </summary>
        public int Height
        {
            get;
        }
    }
}

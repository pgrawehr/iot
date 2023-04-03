// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Graphics
{
    /// <summary>
    /// Registration for the image factory implementation adapter.
    /// Only one factory can be registered at a time.
    /// </summary>
    public static class ImageFactoryRegistry
    {
        private static IImageFactory? s_currentFactory;

        /// <summary>
        /// Register an image factory.
        /// </summary>
        /// <param name="factory">The image factory to register</param>
        public static void RegisterImageFactory(IImageFactory factory)
        {
            s_currentFactory = factory;
        }

        /// <summary>
        /// Creates a bitmap using the active factory
        /// </summary>
        /// <param name="width">Width of the image, in pixels</param>
        /// <param name="height">Height of the image, in pixels</param>
        /// <param name="pixelFormat">Desired pixel format</param>
        /// <returns>A new bitmap with the provided size</returns>
        public static BitmapImage CreateBitmap(int width, int height, PixelFormat pixelFormat)
        {
            VerifyFactoryAvailable();
            return s_currentFactory!.CreateBitmap(width, height, pixelFormat);
        }

        private static void VerifyFactoryAvailable()
        {
            if (s_currentFactory == null)
            {
                throw new InvalidOperationException("No image factory registered. Call ImageFactoryRegistry.RegisterImageFactory() with a suitable implementation first");
            }
        }

        /// <summary>
        /// Create a bitmap from a file
        /// </summary>
        /// <param name="filename">The file to load</param>
        /// <returns>A bitmap</returns>
        public static BitmapImage CreateFromFile(string filename)
        {
            VerifyFactoryAvailable();
            using var s = new FileStream(filename, FileMode.Open);
            return s_currentFactory!.CreateFromStream(s);
        }

        /// <summary>
        /// Create a bitmap from an open stream
        /// </summary>
        /// <param name="data">The data stream</param>
        /// <returns>A bitmap</returns>
        public static BitmapImage CreateFromStream(Stream data)
        {
            VerifyFactoryAvailable();
            return s_currentFactory!.CreateFromStream(data);
        }
    }
}

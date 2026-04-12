// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;

namespace ArduinoCsCompiler.Runtime
{
    // The original implementation of this one is just too bloated. We do not need all that error message lookup stuff
    [ArduinoReplacement(typeof(System.Resources.ResourceManager), true)]
    internal class MiniResourceManager
    {
        public static readonly int MagicNumber = -1091581234;
#pragma warning disable SA1306
        protected Assembly? MainAssembly;    // Need the assembly manifest sometimes.
        protected string BaseNameField; // The field is protected for .NET Framework compatibility
#pragma warning restore SA1306

        public string GetString(string resourceName)
        {
            return resourceName;
        }

        public string GetString(string resourceName, CultureInfo culture)
        {
            return resourceName;
        }

        public MiniResourceManager(Type resourceSource)
        {
            BaseNameField = "MainAssembly";
        }

        internal static bool IsDefaultType(string asmTypeName, string typeName)
        {
            return true;
        }
    }
}

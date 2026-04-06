// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler.Runtime
{
    [ArduinoReplacement("System.Runtime.CompilerServices.Unsafe", "System.Private.CoreLib.dll", false,
        IncludingPrivates = true, TargetFramework = TargetFramework.Nano)]
    internal static class MiniUnsafe2
    {
        [ArduinoImplementation(IgnoreGenericTypes = true)]
        public static ref TTo As<TFrom, TTo>(ref TFrom source)
        {
            // Will need an IL-only implementation
            throw new PlatformNotSupportedException();

            // ldarg.0
            // ret
        }

        [ArduinoImplementation]
        public static ref T Add<T>(ref T source, int elementOffset)
        {
            return ref AddByteOffset(ref source, (IntPtr)(elementOffset * (int)SizeOf<T>()));
        }

        [ArduinoImplementation]
        public static int SizeOf<T>()
        {
            return SizeOfType(typeof(T));
            // sizeof !!0
            // ret
        }

        [ArduinoImplementation]
        public static int SizeOfType(Type t)
        {
            throw new NotImplementedException();
        }

        [ArduinoImplementation(CompareByParameterNames = true)]
        public static ref T AddByteOffset<T>(ref T source, IntPtr byteOffset)
        {
            // This method is implemented by the toolchain
            throw new PlatformNotSupportedException();

            // ldarg.0
            // ldarg.1
            // add
            // ret
        }

        /// <summary>
        /// Adds an element offset to the given reference.
        /// </summary>
        [ArduinoImplementation]
        public static ref T Add<T>(ref T source, IntPtr elementOffset)
        {
            return ref AddByteOffset(ref source, (IntPtr)((uint)elementOffset * (uint)SizeOf<T>()));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler.Runtime
{
    [ArduinoReplacement(typeof(System.Runtime.CompilerServices.RuntimeHelpers), false, IncludingPrivates = true, TargetFramework = TargetFramework.Nano)]
    internal class MiniRuntimeHelpers2
    {
        [ArduinoImplementation]
        public static bool IsBitwiseEquatable<T>()
        {
            return IsBitwiseEquatableCore(typeof(T));
        }

        [ArduinoImplementation]
        private static bool IsBitwiseEquatableCore(Type t)
        {
            return true;
        }
    }
}

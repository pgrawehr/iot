// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler.Runtime
{
    [ArduinoReplacement("System.Collections.Generic.ComparerHelpers", null, true, typeof(System.Collections.Generic.Comparer<>),
        TargetFramework = TargetFramework.Nano)]
    public static class ComparerHelpers
    {
        [ArduinoImplementation]
        public static object CreateDefaultComparer(Type type)
        {
            throw new NotImplementedException("TODO");
        }

        [ArduinoImplementation]
        public static object CreateDefaultEqualityComparer(Type type)
        {
            throw new NotImplementedException("TODO");
        }
    }
}

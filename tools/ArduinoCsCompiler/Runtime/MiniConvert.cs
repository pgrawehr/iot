// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler.Runtime
{
    [ArduinoReplacement(typeof(System.Convert), true, IncludingPrivates = true)]
    internal static class MiniConvert
    {
        [ArduinoImplementation]
        public static int ToInt32(object? value)
        {
            if (value == null)
            {
                return 0;
            }

            return (int)value;
        }
    }
}

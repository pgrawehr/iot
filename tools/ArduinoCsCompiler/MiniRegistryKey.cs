// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler
{
    [ArduinoReplacement(typeof(Microsoft.Win32.Registry), true, TargetFramework = TargetFramework.Nano)]
    internal class MiniRegistryKey
    {
    }
}

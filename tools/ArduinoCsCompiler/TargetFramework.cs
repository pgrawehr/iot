// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ArduinoCsCompiler
{
    [Flags]
    public enum TargetFramework
    {
        Any = 0,
        Firmata = 1,
        Nano = 2,
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler.Runtime
{
    [ArduinoReplacement(typeof(System.IO.Ports.SerialPort), true, TargetFramework = TargetFramework.Nano)]
    internal class MiniSerialPort2
    {
        [ArduinoImplementation]
        public static string[] GetPortNames()
        {
            // Todo: Use nanoframework.System.IO.Ports instead
            return new string[]
            {
                "COM1"
            };
        }
    }
}

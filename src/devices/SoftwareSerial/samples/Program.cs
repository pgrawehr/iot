// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Device.Gpio;
using System.IO.Ports;
using System.Text;
using System.Threading;
using Iot.Device.Serial;

Console.WriteLine("Round trip test with Software Serial Port!");

using SoftwareSerial serial = new SoftwareSerial(7, 8, true, 4800, Parity.None, 8, StopBits.One, new GpioController(), true);

byte[] buffer = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");
serial.BaseStream.Write(buffer, 0, buffer.Length);

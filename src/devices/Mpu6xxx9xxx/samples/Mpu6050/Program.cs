// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Device.I2c;
using System.Numerics;
using Iot.Device.Arduino;
using Iot.Device.Imu;

using ArduinoBoard board = new ArduinoBoard("COM5", 115200);
I2cConnectionSettings settings = new(busId: 0, deviceAddress: Mpu6050.DefaultI2cAddress);
using (Mpu6050 ag = new(board.CreateI2cDevice(settings)))
{
    Console.WriteLine($"Internal temperature: {ag.GetTemperature().DegreesCelsius} C");

    while (!Console.KeyAvailable)
    {
        var acc = ag.GetAccelerometer();
        var gyr = ag.GetGyroscopeReading();
        Console.WriteLine($"Accelerometer data x:{acc.X} y:{acc.Y} z:{acc.Z}");
        Console.WriteLine($"Gyroscope data x:{gyr.X} y:{gyr.Y} z:{gyr.Z}\n");
        Thread.Sleep(100);
    }
}

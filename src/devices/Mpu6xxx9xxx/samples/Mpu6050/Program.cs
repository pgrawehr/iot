// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Device.I2c;
using System.Numerics;
using Iot.Device.Arduino;
using Iot.Device.Imu;
using UnitsNet;
using UnitsNet.Units;

using ArduinoBoard board = new ArduinoBoard("COM5", 115200);
I2cConnectionSettings settings = new(busId: 0, deviceAddress: Mpu6050.DefaultI2cAddress);
using (Mpu6050 ag = new(board.CreateI2cDevice(settings)))
{
    Console.WriteLine($"Internal temperature: {ag.GetTemperature().DegreesCelsius} C");
    Console.WriteLine($"Standard gravity: {Mpu6050.EarthAcceleration.ToUnit(AccelerationUnit.MeterPerSecondSquared)}");

    while (!Console.KeyAvailable)
    {
        var acc = ag.GetAcceleration();
        var gyr = ag.GetRotationalSpeeds();
        Console.WriteLine($"Accelerometer data x:{acc[0]} y:{acc[1]} z:{acc[2]}");
        Acceleration sum = Acceleration.FromStandardGravity(
            Math.Sqrt(acc[0].StandardGravity * acc[0].StandardGravity +
                           acc[1].StandardGravity * acc[1].StandardGravity +
                           acc[2].StandardGravity * acc[2].StandardGravity));
        Console.WriteLine($"Total accel: {sum}");
        Console.WriteLine($"Gyroscope data x:{gyr[0]} y:{gyr[1]} z:{gyr[2]}\n");
        Thread.Sleep(100);
    }
}

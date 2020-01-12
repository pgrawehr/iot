// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Iot.Device.Imu;
using Xunit;

namespace Iot.Device.Imu.Tests
{
    public class Ig500SensorTests
    {
        [Fact]
        public void InitialisationFailsNoAnswer()
        {
            MemoryStream ms = new MemoryStream();
            using (Ig500Sensor sensor = new Ig500Sensor(ms, OutputDataSets.Euler | OutputDataSets.Temperatures))
            {
                // No answer expected
                Assert.False(sensor.WaitForSensorReady(out string errorMessage, TimeSpan.FromSeconds(1)));
            }

        }
    }
}

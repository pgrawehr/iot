using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Iot.Device.Nmea0183.Tests
{
    public sealed class MagneticDeviationCorrectionTest
    {
        [Fact]
        public void CreateDeviationTable()
        {
            MagneticDeviationCorrection dev = new MagneticDeviationCorrection();
            dev.CreateCorrectionTable("D:\\Exchange\\Nmea-2020-06-01-16-41.txt");
        }
    }
}

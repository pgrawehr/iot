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
            dev.CreateCorrectionTable(new string[]
            {
                "..\\..\\..\\Nmea-2020-06-13-10-28.txt",
                "..\\..\\..\\Nmea-2020-06-13-11-09.txt",
                "..\\..\\..\\Nmea-2020-06-13-11-50.txt",
                "..\\..\\..\\Nmea-2020-06-13-12-39.txt",
            });
        }
    }
}

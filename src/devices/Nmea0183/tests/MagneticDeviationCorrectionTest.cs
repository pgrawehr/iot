using System;
using System.Collections.Generic;
using System.Text;
using UnitsNet;
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
                "..\\..\\..\\Nmea-2020-07-23-12-02.txt",
            });

            dev.Save("..\\..\\..\\Calibration_Cirrus.xml", "Cirrus", "HBY5127", "269110660");
        }

        [Fact]
        public void CreateDeviationTable2()
        {
            MagneticDeviationCorrection dev = new MagneticDeviationCorrection();
            dev.CreateCorrectionTable(new string[]
            {
                "..\\..\\..\\Nmea-2020-09-23-07-31.txt",
                "..\\..\\..\\Nmea-2020-09-23-07-53.txt"
            });

            dev.Save("..\\..\\..\\Calibration_Cirrus_v2_deltas.xml", "Cirrus", "HBY5127", "269110660");
        }

        [Fact]
        public void ReadAndUseDeviationTable()
        {
            MagneticDeviationCorrection dev = new MagneticDeviationCorrection();
            dev.Load("..\\..\\..\\Calibration_Cirrus.xml");

            Assert.True(dev.Identification != null);
            Assert.Equal("Cirrus", dev.Identification.ShipName);
            Assert.Equal(316.743820953369, dev.ToMagneticHeading(Angle.FromDegrees(303.3)).Degrees, 3);
            Assert.Equal(301.097492027283, dev.FromMagneticHeading(Angle.FromDegrees(316.743820953369)).Degrees, 3);

            // For all angles, converting back and forth should result in a small delta (not exactly zero though, since the
            // operation is not exactly invertible)
            for (double d = 0.5; d < 361; d += 1.0)
            {
                Angle backAndForth = dev.FromMagneticHeading(dev.ToMagneticHeading(Angle.FromDegrees(d)));
                Angle delta = backAndForth - Angle.FromDegrees(d);
                Assert.True(Math.Abs(delta.Normalize(false).Degrees) < 8);
            }
        }
    }
}

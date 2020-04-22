// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.I2c;
using System.Threading;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using Iot.Device.Common;
using Iot.Units;

namespace Iot.Device.Samples
{
    /// <summary>
    /// Sample program for reading <see cref="Bme680"/> sensor data on a Raspberry Pi.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point for the program.
        /// </summary>
        public static void Main()
        {
            Console.WriteLine("Hello BME680!");

            // The I2C bus ID on the Raspberry Pi 3.
            const int busId = 1;
            // set this to the current sea level pressure in the area for correct altitude readings
            var defaultSeaLevelPressure = Pressure.MeanSeaLevel;

            double stationAltitude = 640; // Set this to the height of your place for correct pressure corrections

            var i2cSettings = new I2cConnectionSettings(busId, Bme680.DefaultI2cAddress);
            var i2cDevice = I2cDevice.Create(i2cSettings);

            using (var bme680 = new Bme680(i2cDevice))
            {
                double gasResistance = Double.NaN;

                while (!Console.KeyAvailable)
                {
                    // get the time a measurement will take with the current settings
                    // change the settings
                    bme680.TemperatureSampling = Sampling.HighResolution;
                    bme680.HumiditySampling = Sampling.HighResolution;
                    bme680.PressureSampling = Sampling.HighResolution;

                    bme680.SetPowerMode(Bme680PowerMode.Forced);

                    for (int i = 0; i < 10; i++)
                    {
                        // perform the measurement
                        bme680.SetPowerMode(Bme680PowerMode.Forced);

                        var measurementDuration = bme680.GetMeasurementDuration(bme680.HeaterProfile);
                        Thread.Sleep(measurementDuration);

                        // Print out the measured data
                        bme680.TryReadTemperature(out var tempValue);
                        bme680.TryReadPressure(out var preValue);
                        bme680.TryReadHumidity(out var humValue);
                        if (bme680.GasConversionIsEnabled)
                        {
                            int loops = 10;
                            while (!bme680.TryReadGasResistance(out gasResistance) && loops-- > 0)
                            {
                                Thread.Sleep(10);
                            }
                        }

                        Console.WriteLine($"Gas resistance: {gasResistance:0.##}Ohm");
                        Console.WriteLine($"Temperature: {tempValue.Celsius:0.#}\u00B0C");
                        Console.WriteLine($"Pressure: {preValue.Hectopascal:0.##}hPa");
                        Console.WriteLine($"Station altitude: {stationAltitude:0.##}m");
                        Console.WriteLine($"Relative humidity: {humValue:0.#}%");

                        // WeatherHelper supports more calculations, such as the summer simmer index, saturated vapor pressure, actual vapor pressure and absolute humidity.
                        Console.WriteLine($"Heat index: {WeatherHelper.CalculateHeatIndex(tempValue, humValue).Celsius:0.#}\u00B0C");
                        Console.WriteLine($"Dew point: {WeatherHelper.CalculateDewPoint(tempValue, humValue).Celsius:0.#}\u00B0C");

                        var baroPress = WeatherHelper.CalculateBarometricPressure(preValue, tempValue, stationAltitude, humValue);
                        Console.WriteLine($"Barometric pressure: {baroPress.Hectopascal:0.##}hPa");
                        Thread.Sleep(1000);
                    }

                    // reset will change settings back to default
                    bme680.Reset();
                }

                bme680.HeaterIsEnabled = false;
                bme680.GasConversionIsEnabled = false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    public static class PredefinedFusionOperations
    {
        public static void LoadPredefinedOperations(this SensorFusionEngine engine)
        {
            engine.RegisterFusionOperation(new List<SensorMeasurement>()
                {
                    SensorMeasurement.AirPressureRawInside,
                    SensorMeasurement.AirPressureRawOutside, SensorMeasurement.AirTemperatureOutside,
                    SensorMeasurement.AltitudeGeoid, SensorMeasurement.AirHumidityOutside,
                },
                (args) =>
                {
                    // Take either pressure, and otherwise the outside values. Inside values are not relevant for the atmospheric constellation.
                    if ((args[0].TryGetAs(out Pressure measuredValue) || args[1].TryGetAs(out measuredValue)) &&
                        args[2].TryGetAs(out Temperature measuredTemperature) && args[3].TryGetAs(out Length altitude))
                    {
                        if (args[4].TryGetAs(out RelativeHumidity humidity))
                        {
                            return (WeatherHelper.CalculateBarometricPressure(measuredValue, measuredTemperature,
                                altitude, humidity), false);
                        }
                        else
                        {
                            return (WeatherHelper.CalculateBarometricPressure(measuredValue, measuredTemperature,
                                altitude), false);
                        }
                    }

                    return (null, false); // At least one input not available - skip this operation
                }, SensorMeasurement.AirPressureBarometricOutside);

            engine.RegisterFusionOperation(new List<SensorMeasurement>() { SensorMeasurement.AirTemperatureOutside, SensorMeasurement.AirHumidityOutside },
                (args) =>
                {
                    if (args[0].TryGetAs(out Temperature temperature) &&
                        args[1].TryGetAs(out RelativeHumidity humidity))
                    {
                        return (WeatherHelper.CalculateHeatIndex(temperature, humidity), false);
                    }

                    return (null, false); // At least one input not available - skip this operation
                }, SensorMeasurement.HeatIndex);

            engine.RegisterFusionOperation(new List<SensorMeasurement>() { SensorMeasurement.AirTemperatureOutside, SensorMeasurement.AirHumidityOutside },
                (args) =>
                {
                    if (args[0].TryGetAs(out Temperature temperature) &&
                        args[1].TryGetAs(out RelativeHumidity humidity))
                    {
                        return (WeatherHelper.CalculateDewPoint(temperature, humidity), false);
                    }

                    return (null, false); // At least one input not available - skip this operation
                }, SensorMeasurement.DewPointOutside);

            engine.RegisterFusionOperation(new List<SensorMeasurement>()
                {
                    SensorMeasurement.AirTemperatureInside, SensorMeasurement.AirHumidityInside,
                    SensorMeasurement.AirTemperatureOutside, SensorMeasurement.AirHumidityOutside
                },
                (args) =>
                {
                    SensorMeasurement humidityOutside = args[3];
                    // Only use this rule if no outside humidity sensor is available
                    if (!humidityOutside.Status.HasFlag(SensorMeasurementStatus.NoData) &&
                        !humidityOutside.Status.HasFlag(SensorMeasurementStatus.IndirectResult))
                    {
                        return (null, true);
                    }

                    if (args[0].TryGetAs(out Temperature temperature1) &&
                        args[1].TryGetAs(out RelativeHumidity humidity) &&
                        args[2].TryGetAs(out Temperature temperature2))
                    {
                        // Use the lower temperature from the two sensors - that's likely the outside temp
                        return (WeatherHelper.GetRelativeHumidityFromActualAirTemperature(temperature1, humidity, temperature1 < temperature2 ? temperature1 : temperature2), false);
                    }

                    return (null, false); // At least one input not available - skip this operation
                }, SensorMeasurement.AirHumidityOutside);
        }
    }
}

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
            engine.RegisterFusionOperation(new List<SensorMeasurement>() { SensorMeasurement.AirPressureRawOutside, SensorMeasurement.AirTemperatureOutside, SensorMeasurement.Altitude, SensorMeasurement.AirHumidityOutside },
                (args) =>
                {
                    if (args[0].TryGetAs(out Pressure measuredValue) &&
                        args[1].TryGetAs(out Temperature measuredTemperature) && args[2].TryGetAs(out Length altitude))
                    {
                        if (args[3].TryGetAs(out Ratio humidity))
                        {
                            return WeatherHelper.CalculateBarometricPressure(measuredValue, measuredTemperature,
                                altitude, humidity);
                        }
                        else
                        {
                            return WeatherHelper.CalculateBarometricPressure(measuredValue, measuredTemperature,
                                altitude);
                        }
                    }

                    return null; // At least one input not available - skip this operation
                }, SensorMeasurement.AirPressureBarometricOutside);

            engine.RegisterFusionOperation(new List<SensorMeasurement>() { SensorMeasurement.AirTemperatureOutside, SensorMeasurement.AirHumidityOutside },
                (args) =>
                {
                    if (args[0].TryGetAs(out Temperature temperature) &&
                        args[1].TryGetAs(out Ratio humidity))
                    {
                        return WeatherHelper.CalculateHeatIndex(temperature, humidity);
                    }

                    return null; // At least one input not available - skip this operation
                }, SensorMeasurement.HeatIndex);

            engine.RegisterFusionOperation(new List<SensorMeasurement>() { SensorMeasurement.AirTemperatureOutside, SensorMeasurement.AirHumidityOutside },
                (args) =>
                {
                    if (args[0].TryGetAs(out Temperature temperature) &&
                        args[1].TryGetAs(out Ratio humidity))
                    {
                        return WeatherHelper.CalculateDewPoint(temperature, humidity);
                    }

                    return null; // At least one input not available - skip this operation
                }, SensorMeasurement.DewPointOutside);
        }
    }
}

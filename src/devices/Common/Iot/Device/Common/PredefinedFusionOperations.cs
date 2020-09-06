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
            engine.RegisterFusionOperation(new List<SensorMeasurement>() { SensorMeasurement.AirPressureRawOutside, SensorMeasurement.AirTemperatureOutside, SensorMeasurement.Altitude },
                (args) =>
                {
                    IQuantity result = WeatherHelper.CalculateBarometricPressure(args[0].GetAs<Pressure>(), args[1].GetAs<Temperature>(), args[2].GetAs<Length>());
                    SensorMeasurement.AirPressureBarometricOutside.UpdateValue(result);
                }, SensorMeasurement.AirPressureBarometricOutside);
        }
    }
}

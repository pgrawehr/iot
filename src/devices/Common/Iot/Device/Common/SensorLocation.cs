using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    /// <summary>
    /// Some locations where a sensor could be located. Pick the closest match (some methods may require a sensor of a certain
    /// location to work correctly. That is documented there). Not all types of sensor locations may be meaningful with all kinds of
    /// value types (i.e. a water temperature sensor is useful, a water humidity sensor maybe not...)
    /// </summary>
    public enum SensorLocation
    {
        Undefined,
        Outside,
        Inside,
        Cpu,
        Gpu,
        Mainboard,
        Case,
        ExternalDevice,
        Freezer,
        Heater,
        Engine,
        Gear,
        Hydraulic,
    }
}

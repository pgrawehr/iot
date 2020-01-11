using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Imu
{
    [Flags]
    internal enum OutputDataSets
    {
        None = 0,
        Quaternion = 0x00000001,
        Euler = 0x00000002,
        Matrix = 0x00000004,
        Gyroscopes = 0x00000008,
        Accelerometers = 0x00000010,
        Magnetometers = 0x00000020,
        Temperatures = 0x00000040,
        GyroscopesRaw = 0x00000080,
        AccelerometersRaw = 0x00000100,
        MagnetometersRaw = 0x00000200,
        TemperaturesRaw = 0x00000400,
        TimeSinceReset = 0x00000800,
        DeviceStatus = 0x00001000,
        GpsPosition = 0x00002000,
        GpsNavigation = 0x00004000,
        GpsAccuracy = 0x00008000,
        GpsInfo = 0x00010000,
        BaroAltitude = 0x00020000,
        BaroPressure = 0x00040000,
        Position = 0x00080000,
        Velocity = 0x00100000,
        AttitudeAccuracy = 0x00200000,
        NavigationAccuracy = 0x00400000,
        GyroTemperatures = 0x00800000,
        GyroTemperaturesRaw = 0x01000000,
        UtcTimeReference = 0x02000000,
        MagCalibData = 0x04000000,
        GpsTrueHeading = 0x08000000,
        OdoVelocity = 0x10000000
    }
}

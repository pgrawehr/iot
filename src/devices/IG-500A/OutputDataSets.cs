using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Imu
{
    /// <summary>
    /// Enabled set of values that are output
    /// </summary>
    [Flags]
    public enum OutputDataSets
    {
        /// <summary>
        /// No data
        /// </summary>
        None = 0,

        /// <summary>
        /// Quaternions
        /// </summary>
        Quaternion = 0x00000001,

        /// <summary>
        /// Euler angles
        /// </summary>
        Euler = 0x00000002,

        /// <summary>
        /// Rotation in matrix form
        /// </summary>
        Matrix = 0x00000004,

        /// <summary>
        /// Gyroscope values, calibrated
        /// </summary>
        Gyroscopes = 0x00000008,

        /// <summary>
        /// Accelerometer values, calibrated
        /// </summary>
        Accelerometers = 0x00000010,

        /// <summary>
        /// Magnetometer values, calibrated
        /// </summary>
        Magnetometers = 0x00000020,

        /// <summary>
        /// Temperatures, calibrated
        /// </summary>
        Temperatures = 0x00000040,

        /// <summary>
        /// Gyroscope values, raw
        /// </summary>
        GyroscopesRaw = 0x00000080,

        /// <summary>
        /// Acclerometer values, raw
        /// </summary>
        AccelerometersRaw = 0x00000100,

        /// <summary>
        /// Magnetometer values, raw
        /// </summary>
        MagnetometersRaw = 0x00000200,

        /// <summary>
        /// Temperatures, raw
        /// </summary>
        TemperaturesRaw = 0x00000400,

        /// <summary>
        /// Time since reset, in ms
        /// </summary>
        TimeSinceReset = 0x00000800,

        /// <summary>
        /// Device status
        /// </summary>
        DeviceStatus = 0x00001000,

        /// <summary>
        /// Gps position (all of the following are only available on the IG-500N or IG-500E)
        /// </summary>
        GpsPosition = 0x00002000,

        /// <summary>
        /// Navigation velocity in three directions
        /// </summary>
        GpsNavigation = 0x00004000,

        /// <summary>
        /// Gps accuracy
        /// </summary>
        GpsAccuracy = 0x00008000,

        /// <summary>
        /// Gps information (number of satellites, etc)
        /// </summary>
        GpsInfo = 0x00010000,

        /// <summary>
        /// Barometric altitude
        /// </summary>
        BaroAltitude = 0x00020000,

        /// <summary>
        /// Barometric pressure
        /// </summary>
        BaroPressure = 0x00040000,

        /// <summary>
        /// WGS84 Position
        /// </summary>
        Position = 0x00080000,

        /// <summary>
        /// Velocity
        /// </summary>
        Velocity = 0x00100000,

        /// <summary>
        /// Accuracy of attitude
        /// </summary>
        AttitudeAccuracy = 0x00200000,

        /// <summary>
        /// Navigation accuracy
        /// </summary>
        NavigationAccuracy = 0x00400000,

        /// <summary>
        /// Gyroscope temperatures, calibrated
        /// </summary>
        GyroTemperatures = 0x00800000,

        /// <summary>
        /// Gyroscope raw temperatures
        /// </summary>
        GyroTemperaturesRaw = 0x01000000,

        /// <summary>
        /// Utc time
        /// </summary>
        UtcTimeReference = 0x02000000,

        /// <summary>
        /// Magnetic calibration data
        /// </summary>
        MagCalibData = 0x04000000,

        /// <summary>
        /// Gps true heading
        /// </summary>
        GpsTrueHeading = 0x08000000,

        /// <summary>
        /// Odometric velocity (only if provided externally, only IG-500E)
        /// </summary>
        OdoVelocity = 0x10000000
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Imu
{
    /// <summary>
    /// Enum used to specify the Mpu6050 sensor low-pass-filter bandwith.
    /// </summary>
    public enum Mpu6050GyroBandwidth
    {
        // The following values apply to the Mpu6050 and 6000 only

        /// <summary>
        /// 256 Hz This implies a sampling rate of 8khz, while in all other settings the sampling rate is 1khz.
        /// </summary>
        BandWidth256Hz = 0,

        /// <summary>
        /// 188 Hz
        /// </summary>
        BandWidth188Hz = 1,

        /// <summary>
        /// 98 Hz
        /// </summary>
        BandWidth98Hz = 2,

        /// <summary>
        /// 42 Hz
        /// </summary>
        BandWidth42 = 3,

        /// <summary>
        /// 20 Hz
        /// </summary>
        BandWidth20 = 4,

        /// <summary>
        /// 10 Hz
        /// </summary>
        BandWidth10 = 5,

        /// <summary>
        /// 5 Hz
        /// </summary>
        BandWidth5 = 5
    }
}

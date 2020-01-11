using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Imu
{
    internal enum CommandIds
    {
        None = 0x0,
        Ack = 0x1,
        GetOutputMode = 0x16,
        RetOutputMode = 0x17,
        GetDefaultOutputMask = 0x51,
        RetDefaultOutputMask = 0x52,
        ContinuousDefaultOutput = 0x90
    }
}

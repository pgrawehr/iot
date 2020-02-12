using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Imu
{
    internal enum CommandIds
    {
        None = 0x0,
        Ack = 0x1,
        SetOutputMode = 0x15,
        GetOutputMode = 0x16,
        RetOutputMode = 0x17,
        SetDefaultOutputMask = 0x50,
        GetDefaultOutputMask = 0x51,
        RetDefaultOutputMask = 0x52,
        ContinuousDefaultOutput = 0x90
    }
}

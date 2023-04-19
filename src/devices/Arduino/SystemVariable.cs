using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Arduino
{
    /// <summary>
    /// List of known system variables
    /// </summary>
    public enum SystemVariable
    {
        /// <summary>
        /// Check whether system variables can be queried. Should be true for protocol version 2.7 or later.
        /// </summary>
        FunctionSupportCheck = 0,

        /// <summary>
        /// Query the input buffer size (maximum size of sysex messages)
        /// </summary>
        QueryInputBufferSize = 1,

        /// <summary>
        /// Enter sleep mode (after a timeout). The argument is in minutes. A value of 0 disables an active timer.
        /// The method cannot be used to wake up the MCU, since it might really be asleep and require an external interrupt to wake up.
        /// </summary>
        EnterSleepMode = 102,
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Arduino
{
    /// <summary>
    /// This extension has the simple task of sending the ESP32 to sleep after disconnecting.
    /// This is helpful for battery-powered devices. An external source is required to wake up
    /// from sleep, as the WIFI transmitter is also switched off.
    /// </summary>
    public class Esp32Sleep : ExtendedCommandHandler
    {
        /// <summary>
        /// Enable sleep mode after the client has disconnected and the specified amount of minutes has passed
        /// </summary>
        /// <param name="afterMinutes">Minutes to wait before entering sleep (max 127)</param>
        public void EnterSleepMode(uint afterMinutes)
        {
            var cmd = new FirmataCommandSequence();
            cmd.WriteByte(0x7B); // SCHEDULER_COMMAND
            cmd.WriteByte(1); // Enable sleep after disconnect
            if (afterMinutes > 0x7F)
            {
                afterMinutes = 0x7F;
            }

            cmd.WriteByte((byte)afterMinutes);
            cmd.WriteByte((byte)FirmataCommand.END_SYSEX);
            SendCommand(cmd);
        }
    }
}

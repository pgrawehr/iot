// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Device.Gpio.Drivers
{
    /// <summary>
    /// This overrides the SysFsDriver, when only the interrupt callback methods are required. 
    /// </summary>
    internal class InterruptSysFsDriver : SysFsDriver
    {
        private GpioDriver _gpioDriver;
        public InterruptSysFsDriver(GpioDriver gpioDriver) : base()
        {
            _gpioDriver = gpioDriver;
            StatusUpdateSleepTime = TimeSpan.Zero; // This driver does not need this "magic sleep" as we're directly accessing the hardware registers
            PollingTimeout = TimeSpan.FromMilliseconds(100);
        }

        protected internal override PinValue Read(int pinNumber)
        {
            return _gpioDriver.Read(pinNumber);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // not our instance
                _gpioDriver = null;
            }
            base.Dispose(disposing);
        }
    }
}

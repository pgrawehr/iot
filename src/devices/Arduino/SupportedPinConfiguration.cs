using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;

#pragma warning disable CS1591

namespace Iot.Device.Arduino
{
    public class SupportedPinConfiguration
    {
        public SupportedPinConfiguration(int pin)
        {
            Pin = pin;
            PinModes = new List<SupportedMode>();
            PwmResolutionBits = 0;
            AnalogInputResolutionBits = 1; // binary
        }

        public int Pin
        {
            get;
        }

        public List<SupportedMode> PinModes
        {
            get;
        }

        public int PwmResolutionBits
        {
            get;
            set;
        }

        public int AnalogInputResolutionBits
        {
            get;
            set;
        }
    }
}

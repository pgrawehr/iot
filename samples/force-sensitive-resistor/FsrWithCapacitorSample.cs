﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Device.Gpio;

namespace force_sensitive_resistor
{
    class FsrWithCapacitorSample
    {
        private readonly GpioController _controller = new();
        private int _pinNumber = 18; // set the reading pin number

        public FsrWithCapacitorSample()
        {
            _controller.OpenPin(_pinNumber);
        }

        public int ReadCapacitorChargingDuration()
        {
            int count = 0;
            _controller.SetPinMode(_pinNumber, PinMode.Output);
            // set pin to low
            _controller.Write(_pinNumber, PinValue.Low);
            // Prepare pin for input and wait until high read
            _controller.SetPinMode(_pinNumber, PinMode.Input);

            while (_controller.Read(_pinNumber) == PinValue.Low)
            {   // count until read High
                count++;
                if (count == 30000)
                {   // if count goes too high it means FSR in highest resistance which means no pressure, do not need to count more
                    break;
                }
            }
            return count;
        }
    }
}

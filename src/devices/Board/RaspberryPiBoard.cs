﻿using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.I2c;
using System.Device.Pwm;
using System.Device.Spi;
using System.Text;

namespace Iot.Device.Board
{
    public class RaspberryPiBoard : GenericBoard
    {
        private ManagedGpioDriver _managedGpioDriver;

        public RaspberryPiBoard(PinNumberingScheme defaultNumberingScheme)
            : base(defaultNumberingScheme)
        {
            // TODO: Ideally detect board type, so that invalid combinations can be prevented (i.e. I2C bus 2 on Raspi 3)
            PinCount = 28;
        }

        public int PinCount
        {
            get;
            protected set;
        }

        public override void Initialize()
        {
            // Needs to be a raspi 3 driver here (either unix or windows)
            _managedGpioDriver = new ManagedGpioDriver(this, new RaspberryPi3Driver(), null);
            base.Initialize();
        }

        public override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
        {
            return pinNumber switch
            {
                3 => 2,
                5 => 3,
                7 => 4,
                8 => 14,
                10 => 15,
                11 => 17,
                12 => 18,
                13 => 27,
                15 => 22,
                16 => 23,
                18 => 24,
                19 => 10,
                21 => 9,
                22 => 25,
                23 => 11,
                24 => 8,
                26 => 7,
                27 => 0,
                28 => 1,
                29 => 5,
                31 => 6,
                32 => 12,
                33 => 13,
                35 => 19,
                36 => 16,
                37 => 26,
                38 => 20,
                40 => 21,
                _ => throw new ArgumentException($"Board (header) pin {pinNumber} is not a GPIO pin on the {GetType().Name} device.", nameof(pinNumber))
            };
        }

        public override int ConvertLogicalNumberingSchemeToPinNumber(int pinNumber)
        {
            return pinNumber switch
            {
                2 => 3,
                3 => 5,
                4 => 7,
                14 => 8,
                15 => 10,
                17 => 11,
                18 => 12,
                27 => 13,
                22 => 15,
                23 => 16,
                24 => 18,
                10 => 19,
                9 => 21,
                25 => 22,
                11 => 23,
                8 => 24,
                7 => 26,
                0 => 27,
                1 => 28,
                5 => 29,
                6 => 31,
                12 => 23,
                13 => 33,
                19 => 35,
                16 => 36,
                26 => 37,
                20 => 38,
                21 => 40,
                _ => throw new ArgumentException($"Board (header) pin {pinNumber} is not a GPIO pin on the {GetType().Name} device.", nameof(pinNumber))
            };
        }

        public override GpioController CreateGpioController(int[] pinAssignment = null)
        {
            return new GpioController(PinNumberingScheme.Logical, new ManagedGpioDriver(this, new RaspberryPi3Driver(), pinAssignment));
        }

        protected override int[] GetDefaultPinAssignmentForI2c(I2cConnectionSettings connectionSettings)
        {
            int scl;
            int sda;
            switch (connectionSettings.BusId)
            {
                case 0:
                {
                    // Bus 0 is the one on logical pins 0 and 1. According to the docs, it should not
                    // be used by application software and instead is reserved for HATs, but if you don't have one, it is free for other purposes
                    sda = 0;
                    scl = 1;
                    break;
                }

                case 1:
                {
                    // This is the bus commonly used by application software.
                    sda = 2;
                    scl = 3;
                    break;
                }

                case 2:
                {
                    throw new NotSupportedException("I2C Bus number 2 doesn't exist");
                }

                case 3:
                {
                    sda = 4;
                    scl = 5;
                    break;
                }

                case 4:
                {
                    sda = 6;
                    scl = 7;
                    break;
                }

                case 5:
                {
                    sda = 10;
                    scl = 11;
                    break;
                }

                case 6:
                    sda = 22;
                    scl = 23;
                    break;

                default:
                    throw new NotSupportedException($"I2C bus {connectionSettings.BusId} does not exist.");
            }

            return new int[]
            {
                sda, scl
            };
        }

        public override AlternatePinMode GetHardwareModeForPinUsage(int pinNumber, PinUsage usage, PinNumberingScheme pinNumberingScheme = PinNumberingScheme.Logical, int bus = 0)
        {
            pinNumber = RemapPin(pinNumber, pinNumberingScheme);
            if (pinNumber >= PinCount)
            {
                return AlternatePinMode.NotSupported;
            }

            if (usage == PinUsage.Gpio)
            {
                // all pins support GPIO
                return AlternatePinMode.Gpio;
            }

            if (usage == PinUsage.I2c)
            {
                // The Pi4 has a big number of pins that can become I2C pins
                switch (pinNumber)
                {
                    // Busses 0 and 1 run on Alt0
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        return AlternatePinMode.Alt0;
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                        return AlternatePinMode.Alt5;
                    case 22:
                    case 23:
                        return AlternatePinMode.Alt5;
                }

                return AlternatePinMode.NotSupported;
            }

            if (usage == PinUsage.Pwm)
            {
                if (pinNumber == 12 || pinNumber == 13)
                {
                    return AlternatePinMode.Alt0;
                }

                if (pinNumber == 18 || pinNumber == 19)
                {
                    return AlternatePinMode.Alt5;
                }

                return AlternatePinMode.NotSupported;
            }

            return AlternatePinMode.NotSupported;
        }

        protected override int GetDefaultPinAssignmentForPwm(int chip, int channel)
        {
            // The default assignment is 12 & 13, but 18 and 19 is supported as well
            if (chip == 0 && channel == 0)
            {
                return 12;
            }

            if (chip == 0 && channel == 1)
            {
                return 13;
            }

            throw new NotSupportedException($"No such PWM Channel: Chip {chip} channel {channel}.");
        }

        protected override void ActivatePinMode(int pinNumber, PinUsage usage)
        {
            AlternatePinMode modeToSet = GetHardwareModeForPinUsage(pinNumber, usage, PinNumberingScheme.Logical);
            if (modeToSet != AlternatePinMode.Unknown)
            {
                _managedGpioDriver.SetAlternatePinMode(pinNumber, modeToSet);
            }

            base.ActivatePinMode(pinNumber, usage);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _managedGpioDriver?.Dispose();
                _managedGpioDriver = null;
            }

            base.Dispose(disposing);
        }
    }
}
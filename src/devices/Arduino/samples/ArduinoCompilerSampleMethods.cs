using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;
using Iot.Device.Arduino;

#pragma warning disable CS1591
namespace Arduino.Samples
{
    /// <summary>
    /// These are simple methods to test the IL execution engine on the Arduino
    /// </summary>
    public class ArduinoCompilerSampleMethods
    {
        public static int AddInts(int a, int b)
        {
            return a + b;
        }

        public static int SubtactInts(int a, int b)
        {
            return a - b;
        }

        public static int Max(int a, int b)
        {
            if (a > b)
            {
                return a;
            }
            else
            {
                return b;
            }
        }

        public static bool Equal(int a, int b)
        {
            return a == b;
        }

        public static bool Unequal(int a, int b)
        {
            return !(a == b);
        }

        public static bool Smaller(int a, int b)
        {
            return a < b;
        }

        public static void Blink(IArduinoHardwareLevelAccess hw, int pin, int delay)
        {
            hw.SetPinMode(pin, PinMode.Output);
            for (int i = 0; Smaller(i, 10); i++)
            {
                hw.WritePin(pin, 1);
                ArduinoRuntimeCore.Sleep(hw, delay);
                hw.WritePin(pin, 0);
                ArduinoRuntimeCore.Sleep(hw, delay);
            }

            hw.SetPinMode(pin, PinMode.Input);
        }

        public static UInt32 ReadDht11(IArduinoHardwareLevelAccess controller, int pin)
        {
            uint count;
            uint resultLow = 0;
            uint result = 0;
            uint checksum = 0;

            uint loopCount = 1000;
            // keep data line HIGH
            controller.SetPinMode(pin, PinMode.Output);
            controller.WritePin(pin, 1);
            ArduinoRuntimeCore.Sleep(controller, 20);

            // send trigger signal
            controller.WritePin(pin, 0);
            // wait at least 18 milliseconds
            // here wait for 18 milliseconds will cause sensor initialization to fail
            ArduinoRuntimeCore.Sleep(controller, 20);

            // pull up data line
            controller.WritePin(pin, 1);
            // wait 20 - 40 microseconds
            controller.SleepMicroseconds(20);

            controller.SetPinMode(pin, PinMode.InputPullUp);

            // DHT corresponding signal - LOW - about 80 microseconds
            count = loopCount;
            while (controller.ReadPin(pin) == 0)
            {
                if (count-- == 0)
                {
                    return 0;
                }
            }

            // HIGH - about 80 microseconds
            count = loopCount;
            while (controller.ReadPin(pin) == 1)
            {
                if (count-- == 0)
                {
                    return 0;
                }
            }

            // the read data contains 40 bits
            for (int i = 0; i < 40; i++)
            {
                // beginning signal per bit, about 50 microseconds
                count = loopCount;
                while (controller.ReadPin(pin) == 0)
                {
                    if (count-- == 0)
                    {
                        return 0;
                    }
                }

                // 26 - 28 microseconds represent 0
                // 70 microseconds represent 1
                UInt32 watchStart = controller.GetMicroseconds();
                count = loopCount;
                while (controller.ReadPin(pin) == 1)
                {
                    if (count-- == 0)
                    {
                        return 0;
                    }
                }

                UInt32 elapsed = controller.GetMicroseconds() - watchStart;

                // bit to byte
                // less than 40 microseconds can be considered as 0, not necessarily less than 28 microseconds
                // here take 30 microseconds
                resultLow <<= 1;
                if (!(elapsed * 1000000 / 1_000_000 <= 30))
                {
                    resultLow |= 1;
                }

                if (i == 31)
                {
                    // When 32 bits have been read, save them away - what follows is the checksum
                    result = resultLow;
                    resultLow = 0;
                }
            }

            checksum = resultLow;
            ////_lastMeasurement = Environment.TickCount;

            ////if ((_readBuff[4] == ((_readBuff[0] + _readBuff[1] + _readBuff[2] + _readBuff[3]) & 0xFF)))
            ////{
            ////    IsLastReadSuccessful = (_readBuff[0] != 0) || (_readBuff[2] != 0);
            ////}
            ////else
            ////{
            ////    IsLastReadSuccessful = false;
            ////}

            return result;
        }
    }
}

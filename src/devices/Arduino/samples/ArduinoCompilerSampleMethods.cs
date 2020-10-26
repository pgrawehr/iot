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
    }
}

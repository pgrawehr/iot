using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;

#pragma warning disable CS1591
namespace Iot.Device.Arduino
{
    public class ArduinoCompilerMethods
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

        public static void Blink(IArduinoHardwareLevelAccess hw, int pin, int delay)
        {
            hw.SetPinMode(pin, PinMode.Output);
            for (int i = 0; i < 10; i++)
            {
                hw.WritePin(pin, 1);
                hw.Sleep(delay);
                hw.WritePin(pin, 0);
                hw.Sleep(delay);
            }

            hw.SetPinMode(pin, PinMode.Input);
        }
    }
}

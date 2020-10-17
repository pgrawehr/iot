using System;
using System.Collections.Generic;
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

        public static int Larger(int a, int b)
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
    }
}

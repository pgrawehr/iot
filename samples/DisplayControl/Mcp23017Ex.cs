using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using Iot.Device.Mcp23xxx;

namespace DisplayControl
{
    public class Mcp23017Ex : Mcp23017
    {
        public Mcp23017Ex(I2cDevice i2cDevice, int reset = -1, int interruptA = -1, int interruptB = -1, GpioController masterController = null, bool shouldDispose = true) 
            : base(i2cDevice, reset, interruptA, interruptB, masterController, shouldDispose)
        {
        }

        public int ReadPortB()
        {
            Span<PinValuePair> data = stackalloc PinValuePair[]
            {
                new PinValuePair(8, default), new PinValuePair(9, default), new PinValuePair(10, default), new PinValuePair(11, default), new PinValuePair(12, default), new PinValuePair(13, default), new PinValuePair(14, default), new PinValuePair(15, default),
            };

            Read(data);

            int value = 0;
            for (int i = 0; i < data.Length; i++)
            {
                value = value | (((int)data[i].PinValue) << i);
            }

            return value;
        }
    }
}

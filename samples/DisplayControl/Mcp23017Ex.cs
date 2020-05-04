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
            int value = ReadByte(Register.GPIO, Port.PortB);

            return value;
        }
    }
}

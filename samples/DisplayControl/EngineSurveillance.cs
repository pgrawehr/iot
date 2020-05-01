using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using Iot.Device.Mcp23xxx;

namespace DisplayControl
{
    public class EngineSurveillance : PollingSensorBase
    {
        private I2cDevice _device;
        private Mcp23017 _mcp23017;
        private GpioController _controllerUsingMcp;

        /// <summary>
        /// Create an instance of this class.
        /// Note: Adapt polling timeout when needed
        /// </summary>
        public EngineSurveillance() 
            : base(TimeSpan.FromSeconds(5))
        {
        }

        public override void Init(GpioController gpioController)
        {
            base.Init(gpioController);
            _device = I2cDevice.Create(new I2cConnectionSettings(1, 0x21));
            _mcp23017 = new Mcp23017(_device, -1, -1, -1, gpioController);
            _controllerUsingMcp = new GpioController(PinNumberingScheme.Logical, _mcp23017);

            // Just open all the pins
            for (int i = 0; i < _controllerUsingMcp.PinCount; i++)
            {
                _controllerUsingMcp.OpenPin(i);
            }
        }

        protected override void UpdateSensors()
        {
        }
    }
}

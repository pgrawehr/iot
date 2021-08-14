using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Arduino;
using Iot.Device.Common;
using Microsoft.Extensions.Logging;

namespace DisplayControl
{
    public class ArduinoSensors : PollingSensorBase
    {
        private ArduinoBoard _board;
        private ILogger _logger;
        public ArduinoSensors(MeasurementManager manager) : base(manager,
            TimeSpan.FromSeconds(1))
        {
            _logger = this.GetCurrentClassLogger();
        }

        public override void Init(GpioController gpioController)
        {
            if (!ArduinoBoard.TryFindBoard(SerialPort.GetPortNames(), new int[] { 115200 }, out _board))
            {
                throw new NotSupportedException("Could not find Arduino");
            }

            _logger.LogInformation($"Connected to Arduino, {_board.FirmwareName} {_board.FirmwareVersion}");
            base.Init(gpioController);
        }

        protected override void UpdateSensors()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _board?.Dispose();
                _board = null;
            }

            base.Dispose(disposing);
        }
    }
}

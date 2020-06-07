using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Text;

namespace Iot.Device.Board
{
    internal class I2cDeviceManager : I2cDevice
    {
        private readonly Board _board;
        private readonly int _sdaPin;
        private readonly int _sclPin;
        private readonly I2cDevice _i2cDeviceImplementation;

        public I2cDeviceManager(Board board, I2cConnectionSettings settings, int[] pins)
        {
            _board = board;
            _sdaPin = pins[0];
            _sclPin = pins[1];
            try
            {
                _board.ReservePin(_sdaPin, PinUsage.I2c, this);
                _board.ReservePin(_sclPin, PinUsage.I2c, this);
                _i2cDeviceImplementation = I2cDevice.Create(settings);
            }
            finally
            {
                _board.ReleasePin(_sdaPin, PinUsage.I2c, this);
                _board.ReleasePin(_sclPin, PinUsage.I2c, this);
            }
        }

        public override I2cConnectionSettings ConnectionSettings
        {
            get
            {
                return _i2cDeviceImplementation.ConnectionSettings;
            }
        }

        public override byte ReadByte()
        {
            return _i2cDeviceImplementation.ReadByte();
        }

        public override void Read(Span<byte> buffer)
        {
            _i2cDeviceImplementation.Read(buffer);
        }

        public override void WriteByte(byte value)
        {
            _i2cDeviceImplementation.WriteByte(value);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _i2cDeviceImplementation.Write(buffer);
        }

        public override void WriteRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
        {
            _i2cDeviceImplementation.WriteRead(writeBuffer, readBuffer);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _i2cDeviceImplementation.Dispose();
                _board.ReleasePin(_sdaPin, PinUsage.I2c, this);
                _board.ReleasePin(_sclPin, PinUsage.I2c, this);
            }

            base.Dispose(disposing);
        }
    }
}

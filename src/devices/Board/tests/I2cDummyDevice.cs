using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;

namespace Board.Tests
{
    internal class I2cDummyDevice : I2cDevice
    {
        public I2cDummyDevice(I2cConnectionSettings settings, int[] pinAssignment)
        {
            ConnectionSettings = settings;
            PinAssignment = pinAssignment;
        }

        public override I2cConnectionSettings ConnectionSettings { get; }
        public int[] PinAssignment { get; }

        public override byte ReadByte()
        {
            throw new Win32Exception(2, "No answer from device");
        }

        public override void Read(Span<byte> buffer)
        {
            throw new Win32Exception(2, "No answer from device");
        }

        public override void WriteByte(byte value)
        {
            throw new Win32Exception(2, "No answer from device");
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            throw new Win32Exception(2, "No answer from device");
        }

        public override void WriteRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
        {
            throw new Win32Exception(2, "No answer from device");
        }
    }
}

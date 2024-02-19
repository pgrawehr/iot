// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Device;
using System.Threading;
using System.Device.Gpio;
using System.IO;
using System.IO.Ports;

#pragma warning disable CS1591
namespace Iot.Device.Serial
{
    /// <summary>
    /// Software Serial Port implementation
    /// </summary>
    public sealed class SoftwareSerial : MarshalByRefObject, IDisposable
    {
        private GpioPin _txPin;
        private GpioPin _rxPin;
        private readonly bool _shouldDispose;
        private GpioController _gpioController;

        private int _frameNumberOfBits;
        private TimeSpan _timePerBit;

        public SoftwareSerial(int txPinNo, int rxPinNo, bool ttlOutput, int baudRate, Parity parity, int dataBits, StopBits stopBits, GpioController gpioController, bool shouldDispose = true)
        {
            _shouldDispose = shouldDispose || gpioController is null;
            _gpioController = gpioController ?? throw new ArgumentNullException(nameof(gpioController));

            TtlOutput = ttlOutput;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            StopBits = stopBits;
            if (dataBits < 5 || dataBits > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(dataBits), "Only 5-8 data bits are allowed");
            }

            if (stopBits == StopBits.OnePointFive)
            {
                throw new ArgumentException("1.5 Stopbits are not supported. For the very rare use cases of this, use 2 Stopbits instead");
            }

            CalculateFrameLength();

            _txPin = _gpioController.OpenPin(txPinNo, PinMode.Output);
            _rxPin = _gpioController.OpenPin(rxPinNo, PinMode.Input);

            _txPin.Write(!TtlOutput); // In ttl mode, "idle" is low
        }

        public int BaudRate { get; }

        /// <summary>
        /// True (the default) to use TTL output levels (low = 0, high = 1), false to use inverse bits (but obviously still at 3.3 or 5.0 V levels)
        /// </summary>
        public bool TtlOutput
        {
            get;
            set;
        }

        public Parity Parity { get; }
        public int DataBits { get; }
        public StopBits StopBits { get; }

        private void CalculateFrameLength()
        {
            int frameBits = 1 + DataBits;
            frameBits += StopBits switch
            {
                StopBits.OnePointFive => 2,
                StopBits.None => 0,
                StopBits.One => 1,
                StopBits.Two => 2,
                _ => 1,
            };

            _frameNumberOfBits = frameBits;

            double timePerBit = 1.0 / BaudRate;
            _timePerBit = TimeSpan.FromSeconds(timePerBit);
        }

        private void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                byte nextByte = buffer[offset++];
                WriteByte(nextByte);
                count--;
            }
        }

        /// <summary>
        /// Waits for the duration of 1 bit
        /// </summary>
        private void Clock()
        {
            DelayHelper.DelayMicroseconds((int)(_timePerBit.TotalMilliseconds * 1000), true);
        }

        private void WriteByte(byte b)
        {
            _txPin.Write(TtlOutput); // Start bit is always the opposite of idle
            Clock();
            int bitMask = 1;
            for (int i = 0; i < DataBits; i++)
            {
                // The data bits are transmitted LSB first!
                int bit = b & bitMask;
                if (TtlOutput)
                {
                    _txPin.Write(bit != 0);
                }
                else
                {
                    _txPin.Write(bit == 0);
                }

                bitMask <<= 1;
                Clock();
            }

            _txPin.Write(!TtlOutput); // Stop bit
        }

        public void Dispose()
        {
            if (_shouldDispose)
            {
                _gpioController?.Dispose();
                _gpioController = null!;
            }
        }

        private sealed class SerialPortStream : Stream
        {
            private readonly SoftwareSerial _mainInterface;

            public SerialPortStream(SoftwareSerial mainInterface)
            {
                _mainInterface = mainInterface;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new InvalidOperationException("Cannot seek on a serial port");

            public override long Position
            {
                get
                {
                    throw new InvalidOperationException("A serial port has no offset");
                }
                set
                {
                    throw new InvalidOperationException("Cannot reposition a serial stream");
                }
            }

            public override void Flush()
            {
                // Nothing to do, we write synchronously
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new InvalidOperationException("Cannot reposition a serial stream");
            }

            public override void SetLength(long value)
            {
                throw new InvalidOperationException("Cannot set the length of a serial stream");
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _mainInterface.Write(buffer, offset, count);
            }
        }
    }
}

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

#pragma warning disable CS1591
namespace Iot.Device.Arduino
{
    /// <summary>
    /// Input/Output stream to a Firmata-Enabled device
    /// </summary>
    public class FirmataStream : Stream
    {
        private Stream _streamImplementation;

        public FirmataStream(Stream serialPort)
        {
            _streamImplementation = serialPort;
        }

        public void Write(byte b)
        {
            _streamImplementation.WriteByte(b);
        }

        public override void Flush()
        {
            _streamImplementation.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _streamImplementation.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _streamImplementation.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _streamImplementation.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _streamImplementation.Write(buffer, offset, count);
        }

        public override bool CanRead => _streamImplementation.CanRead;

        public override bool CanSeek => _streamImplementation.CanSeek;

        public override bool CanWrite => _streamImplementation.CanWrite;

        public override long Length => _streamImplementation.Length;

        public override long Position
        {
            get => _streamImplementation.Position;
            set => _streamImplementation.Position = value;
        }
    }
}

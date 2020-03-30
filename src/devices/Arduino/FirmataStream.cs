using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Iot.Device.Arduino
{
    /// <summary>
    /// Input/Output stream to a Firmata-Enabled device
    /// </summary>
    public abstract class FirmataStream : Stream
    {
        public void flush();

        public void end();

        public void write(byte b);

        public ushort read();
    }
}

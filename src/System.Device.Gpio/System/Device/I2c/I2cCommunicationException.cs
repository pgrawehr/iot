using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#pragma warning disable CS1591
namespace System.Device.Gpio.I2c
{
    /// <summary>
    /// This exception is thrown on I2C communication errors
    /// </summary>
    public class I2cCommunicationException : IOException
    {
        public I2cCommunicationException()
        {
        }

        public I2cCommunicationException(string message)
            : base(message)
        {
        }

        public I2cCommunicationException(string message, System.Exception inner)
            : base(message, inner)
        {
        }

        protected I2cCommunicationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Device.Analog
{
    public abstract class AnalogControllerDriver : IDisposable
    {
        public abstract int PinCount
        {
            get;
        }

        /// <summary>
        /// The reference voltage level, if externally supplied.
        /// Not supported by all boards.
        /// While the Arduino does have an external analog input reference pin, Firmata doesn't allow configuring it.
        /// </summary>
        public double VoltageReference
        {
            get;
            set;
        }

        protected internal abstract int ConvertPinNumberToLogicalNumberingScheme(int pinNumber);

        public abstract void OpenPin(int pinNumber);
        public abstract void ClosePin(int pinNumber);

        /// <summary>
        /// Return the resolution of an analog input pin.
        /// </summary>
        /// <param name="pinNumber">The pin number</param>
        /// <param name="numberOfBits">Returns the resolution of the ADC in number of bits, including the sign bit (if applicable)</param>
        /// <param name="minVoltage">Minimum measurable voltage</param>
        /// <param name="maxVoltage">Maximum measurable voltage</param>
        public abstract void QueryResolution(int pinNumber, out int numberOfBits, out double minVoltage, out double maxVoltage);

        public abstract uint ReadRaw(int pinNumber);

        public abstract double ReadVoltage(int pinNumber);

        public abstract bool SupportsAnalogInput(int pinNumber);

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

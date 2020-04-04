using System.Collections.Generic;
using System.Device.Gpio;

namespace System.Device.Analog
{
    public sealed class AnalogController : IDisposable
    {
        private readonly AnalogControllerDriver _driver;
        private readonly HashSet<int> _openPins;

        /// <summary>
        /// Initializes a new instance of the <see cref="GpioController"/> class that will use the specified numbering scheme and driver.
        /// </summary>
        /// <param name="numberingScheme">The numbering scheme used to represent pins provided by the controller.</param>
        /// <param name="driver">The driver that manages all of the pin operations for the controller.</param>
        public AnalogController(PinNumberingScheme numberingScheme, AnalogControllerDriver driver)
        {
            _driver = driver;
            NumberingScheme = numberingScheme;
            _openPins = new HashSet<int>();
        }

        /// <summary>
        /// The numbering scheme used to represent pins provided by the controller.
        /// </summary>
        public PinNumberingScheme NumberingScheme { get; }

        /// <summary>
        /// The number of pins provided by the controller.
        /// </summary>
        public int PinCount => _driver.PinCount;

        /// <summary>
        /// Reference voltage (the maximum voltage measurable).
        /// For some hardware, it might be necessary to manually set this value for the <see cref="ReadVoltage"/> method to return correct values.
        /// </summary>
        public double VoltageReference
        {
            get
            {
                return _driver.VoltageReference;
            }
            set
            {
                _driver.VoltageReference = value;
            }
        }

        /// <summary>
        /// Gets the logical pin number in the controller's numbering scheme.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The logical pin number in the controller's numbering scheme.</returns>
        private int GetLogicalPinNumber(int pinNumber)
        {
            return (NumberingScheme == PinNumberingScheme.Logical) ? pinNumber : _driver.ConvertPinNumberToLogicalNumberingScheme(pinNumber);
        }

        /// <summary>
        /// Opens a pin in order for it to be ready to use.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        public void OpenPin(int pinNumber)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber);
            if (!_driver.SupportsAnalogInput(logicalPinNumber))
            {
                throw new NotSupportedException($"Pin {pinNumber} is not supporting analog input.");
            }

            if (_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("The selected pin is already open.");
            }

            _driver.OpenPin(logicalPinNumber);
            _openPins.Add(logicalPinNumber);
        }

        /// <summary>
        /// Closes an open pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        public void ClosePin(int pinNumber)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not close a pin that is not open.");
            }

            _driver.ClosePin(logicalPinNumber);
            _openPins.Remove(logicalPinNumber);
        }

        /// <summary>
        /// Checks if a specific pin is open.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The status if the pin is open or closed.</returns>
        public bool IsPinOpen(int pinNumber)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber);
            return _openPins.Contains(logicalPinNumber);
        }

        /// <summary>
        /// Checks if a pin supports a specific mode.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="mode">The mode to check.</param>
        /// <returns>The status if the pin supports the mode.</returns>
        public bool IsPinModeSupported(int pinNumber, PinMode mode)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber);
            return _driver.SupportsAnalogInput(logicalPinNumber);
        }

        /// <summary>
        /// Reads the current raw value of a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The raw value of the pin. Note that the return value is not sign-extended when the value is negative</returns>
        public uint ReadRaw(int pinNumber)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not read from a pin that is not open.");
            }

            return _driver.ReadRaw(logicalPinNumber);
        }

        /// <summary>
        /// Reads the current raw value of a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The voltage at the given pin. Depending on the hardware, the reference voltage must have been properly set. <seealso cref="VoltageReference"/>.</returns>
        public double ReadVoltage(int pinNumber)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not read from a pin that is not open.");
            }

            return _driver.ReadVoltage(logicalPinNumber);
        }

        /*
        /// <summary>
        /// Adds a callback that will be invoked when pinNumber has an event of type eventType.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <param name="callback">The callback method that will be invoked.</param>
        public void RegisterCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not add callback for a pin that is not open.");
            }

            _driver.AddCallbackForAnalogValueChangedEvent(logicalPinNumber, eventTypes, callback);
        }

        /// <summary>
        /// Removes a callback that was being invoked for pin at pinNumber.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="callback">The callback method that will be invoked.</param>
        public void UnregisterCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not add callback for a pin that is not open.");
            }

            _driver.RemoveCallbackForPinValueChangedEvent(logicalPinNumber, callback);
        }
        */
        private void Dispose(bool disposing)
        {
            foreach (int pin in _openPins)
            {
                _driver.ClosePin(pin);
            }

            _openPins.Clear();
            _driver.Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}

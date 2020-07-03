// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Device.Gpio.Drivers;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace System.Device.Gpio
{
    /// <summary>
    /// Represents a general-purpose I/O (GPIO) controller.
    /// </summary>
    public class GpioController : IDisposable
    {
        // Constants used to check the hardware on linux
        private const string CpuInfoPath = "/proc/cpuinfo";
        private const string RaspberryPiHardware = "BCM2835";

        // Constants used to check the hardware on Windows
        private const string BaseBoardProductRegistryValue = @"SYSTEM\HardwareConfig\Current\BaseBoardProduct";
        private const string RaspberryPi2Product = "Raspberry Pi 2";
        private const string RaspberryPi3Product = "Raspberry Pi 3";

        private const string HummingBoardProduct = "HummingBoard-Edge";
        private const string HummingBoardHardware = @"Freescale i.MX6 Quad/DualLite (Device Tree)";

        private readonly GpioDriver _driver;
        private readonly HashSet<int> _openPins;

        /// <summary>
        /// Initializes a new instance of the <see cref="GpioController"/> class that will use the logical pin numbering scheme as default.
        /// </summary>
        public GpioController()
            : this(PinNumberingScheme.Logical)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GpioController"/> class that will use the specified numbering scheme and driver.
        /// </summary>
        /// <param name="numberingScheme">The numbering scheme used to represent pins provided by the controller.</param>
        /// <param name="driver">The driver that manages all of the pin operations for the controller.</param>
        public GpioController(PinNumberingScheme numberingScheme, GpioDriver driver)
        {
            _driver = driver;
            NumberingScheme = numberingScheme;
            _openPins = new HashSet<int>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GpioController"/> class that will use the specified numbering scheme.
        /// The controller will default to use the driver that best applies given the platform the program is executing on.
        /// </summary>
        /// <param name="numberingScheme">The numbering scheme used to represent pins provided by the controller.</param>
        public GpioController(PinNumberingScheme numberingScheme)
            : this(numberingScheme, GetBestDriverForBoard())
        {
        }

        /// <summary>
        /// The numbering scheme used to represent pins provided by the controller.
        /// </summary>
        public PinNumberingScheme NumberingScheme { get; }

        /// <summary>
        /// The number of pins provided by the controller.
        /// </summary>
        public virtual int PinCount => _driver.PinCount;

        /// <summary>
        /// Gets the logical pin number in the controller's numbering scheme.
        /// </summary>
        /// <param name="pinNumber">The pin number</param>
        /// <param name="givenScheme">The scheme for the pin number</param>
        /// <returns>The logical pin number in the controller's numbering scheme.</returns>
        protected virtual int GetLogicalPinNumber(int pinNumber, PinNumberingScheme givenScheme)
        {
            return (givenScheme == PinNumberingScheme.Logical) ? pinNumber : _driver.ConvertPinNumberToLogicalNumberingScheme(pinNumber);
        }

        /// <summary>
        /// Opens a pin in order for it to be ready to use.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        public virtual void OpenPin(int pinNumber)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            if (_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("The selected pin is already open.");
            }

            _driver.OpenPin(logicalPinNumber);
            _openPins.Add(logicalPinNumber);
        }

        /// <summary>
        /// Opens a pin and sets it to a specific mode.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="mode">The mode to be set.</param>
        public virtual void OpenPin(int pinNumber, PinMode mode)
        {
            OpenPin(pinNumber);
            SetPinMode(pinNumber, mode);
        }

        /// <summary>
        /// Closes an open pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        public void ClosePin(int pinNumber)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            ClosePin(pinNumber, NumberingScheme);
        }

        /// <summary>
        /// Closes an open pin.
        /// </summary>
        /// <param name="pinNumber">The pin number.</param>
        /// <param name="numberingScheme">Numbering scheme for the given pin</param>
        /// <remarks>Internal use, to make Dispose work consistently</remarks>
        protected virtual void ClosePin(int pinNumber, PinNumberingScheme numberingScheme)
        {
            if (!_openPins.Contains(pinNumber))
            {
                throw new InvalidOperationException("Can not close a pin that is not open.");
            }

            _driver.ClosePin(pinNumber);
            _openPins.Remove(pinNumber);
        }

        /// <summary>
        /// Sets the mode to a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="mode">The mode to be set.</param>
        public virtual void SetPinMode(int pinNumber, PinMode mode)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not set a mode to a pin that is not open.");
            }

            if (!_driver.IsPinModeSupported(logicalPinNumber, mode))
            {
                throw new InvalidOperationException("The pin does not support the selected mode.");
            }

            _driver.SetPinMode(logicalPinNumber, mode);
        }

        /// <summary>
        /// Gets the mode of a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The mode of the pin.</returns>
        public virtual PinMode GetPinMode(int pinNumber)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not get the mode of a pin that is not open.");
            }

            return _driver.GetPinMode(logicalPinNumber);
        }

        /// <summary>
        /// Checks if a specific pin is open.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The status if the pin is open or closed.</returns>
        public virtual bool IsPinOpen(int pinNumber)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            return _openPins.Contains(logicalPinNumber);
        }

        /// <summary>
        /// Checks if a pin supports a specific mode.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="mode">The mode to check.</param>
        /// <returns>The status if the pin supports the mode.</returns>
        public virtual bool IsPinModeSupported(int pinNumber, PinMode mode)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            return _driver.IsPinModeSupported(logicalPinNumber, mode);
        }

        /// <summary>
        /// Reads the current value of a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The value of the pin.</returns>
        public virtual PinValue Read(int pinNumber)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not read from a pin that is not open.");
            }

            return _driver.Read(logicalPinNumber);
        }

        /// <summary>
        /// Writes a value to a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="value">The value to be written to the pin.</param>
        public virtual void Write(int pinNumber, PinValue value)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not write to a pin that is not open.");
            }

            if (_driver.GetPinMode(logicalPinNumber) != PinMode.Output)
            {
                throw new InvalidOperationException("Can not write to a pin that is not set to Output mode.");
            }

            _driver.Write(logicalPinNumber, value);
        }

        /// <summary>
        /// Returns the currently set pin mode by directly reading the hardware
        /// </summary>
        /// <param name="pinNumber">Pin number</param>
        /// <returns>(Alternate) Pin mode. 0 = Alt0, 1= Alt1... -1 Gpio Input, -2 Gpio Output</returns>
        protected internal int GetAlternatePinMode(int pinNumber)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            // TODO: Maybe use an interface for this feature query instead?
            RaspberryPi3Driver driver = _driver as RaspberryPi3Driver;
            if (driver != null)
            {
                return driver.GetAlternatePinMode(logicalPinNumber);
            }

            throw new NotSupportedException("Driver does not support alternate pin modes");
        }

        /// <summary>
        /// Sets the alternate pin mode for drivers that support this.
        /// </summary>
        /// <param name="pinNumber">The pin number to use</param>
        /// <param name="altMode">Alternate mode (0 = Alt0, 1 = Alt1... anything else = Back to Gpio)</param>
        protected internal void SetAlternatePinMode(int pinNumber, int altMode)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            RaspberryPi3Driver driver = _driver as RaspberryPi3Driver;
            if (driver != null)
            {
                driver.SetAlternatePinMode(logicalPinNumber, altMode);
                return;
            }

            throw new NotSupportedException("Driver does not support alternate pin modes");
        }

        /// <summary>
        /// Blocks execution until an event of type eventType is received or a period of time has expired.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <param name="timeout">The time to wait for the event.</param>
        /// <returns>A structure that contains the result of the waiting operation.</returns>
        public virtual WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, TimeSpan timeout)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource(timeout))
            {
                return WaitForEvent(pinNumber, eventTypes, tokenSource.Token);
            }
        }

        /// <summary>
        /// Blocks execution until an event of type eventType is received or a cancellation is requested.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <param name="cancellationToken">The cancellation token of when the operation should stop waiting for an event.</param>
        /// <returns>A structure that contains the result of the waiting operation.</returns>
        public virtual WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not wait for events from a pin that is not open.");
            }

            return _driver.WaitForEvent(logicalPinNumber, eventTypes, cancellationToken);
        }

        /// <summary>
        /// Async call to wait until an event of type eventType is received or a period of time has expired.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <param name="timeout">The time to wait for the event.</param>
        /// <returns>A task representing the operation of getting the structure that contains the result of the waiting operation.</returns>
        public async ValueTask<WaitForEventResult> WaitForEventAsync(int pinNumber, PinEventTypes eventTypes, TimeSpan timeout)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource(timeout))
            {
                return await WaitForEventAsync(pinNumber, eventTypes, tokenSource.Token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Async call until an event of type eventType is received or a cancellation is requested.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <param name="token">The cancellation token of when the operation should stop waiting for an event.</param>
        /// <returns>A task representing the operation of getting the structure that contains the result of the waiting operation</returns>
        public virtual ValueTask<WaitForEventResult> WaitForEventAsync(int pinNumber, PinEventTypes eventTypes, CancellationToken token)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not wait for events from a pin that is not open.");
            }

            return _driver.WaitForEventAsync(logicalPinNumber, eventTypes, token);
        }

        /// <summary>
        /// Adds a callback that will be invoked when pinNumber has an event of type eventType.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <param name="callback">The callback method that will be invoked.</param>
        public virtual void RegisterCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not add callback for a pin that is not open.");
            }

            _driver.AddCallbackForPinValueChangedEvent(logicalPinNumber, eventTypes, callback);
        }

        /// <summary>
        /// Removes a callback that was being invoked for pin at pinNumber.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="callback">The callback method that will be invoked.</param>
        public virtual void UnregisterCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {
            int logicalPinNumber = GetLogicalPinNumber(pinNumber, NumberingScheme);
            if (!_openPins.Contains(logicalPinNumber))
            {
                throw new InvalidOperationException("Can not remove callback for a pin that is not open.");
            }

            _driver.RemoveCallbackForPinValueChangedEvent(logicalPinNumber, callback);
        }

        /// <summary>
        /// Disposes this instance and closes all open pins associated with this controller.
        /// </summary>
        /// <param name="disposing">True to dispose all instances, false to dispose only unmanaged resources</param>
        protected virtual void Dispose(bool disposing)
        {
            var tempList = new List<int>(_openPins); // Because ClosePin modifies this list
            foreach (int pin in tempList)
            {
                // We need to call this special overload, because the _openPins list always contains logical numbers.
                ClosePin(pin, PinNumberingScheme.Logical);
            }

            _openPins.Clear();
            _driver.Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Write the given pins with the given values.
        /// </summary>
        /// <param name="pinValuePairs">The pin/value pairs to write.</param>
        public void Write(ReadOnlySpan<PinValuePair> pinValuePairs)
        {
            for (int i = 0; i < pinValuePairs.Length; i++)
            {
                Write(pinValuePairs[i].PinNumber, pinValuePairs[i].PinValue);
            }
        }

        /// <summary>
        /// Read the given pins with the given pin numbers.
        /// </summary>
        /// <param name="pinValuePairs">The pin/value pairs to read.</param>
        public void Read(Span<PinValuePair> pinValuePairs)
        {
            for (int i = 0; i < pinValuePairs.Length; i++)
            {
                int pin = pinValuePairs[i].PinNumber;
                pinValuePairs[i] = new PinValuePair(pin, Read(pin));
            }
        }

        /// <summary>
        /// Tries to create the GPIO driver that best matches the current hardware
        /// </summary>
        /// <returns>An instance of a GpioDriver that best matches the current hardware</returns>
        /// <exception cref="PlatformNotSupportedException">No matching driver could be found</exception>
        protected static GpioDriver GetBestDriverForBoard()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return GetBestDriverForBoardOnWindows();
            }
            else
            {
                return GetBestDriverForBoardOnLinux();
            }
        }

        /// <summary>
        /// Attempt to get the best applicable driver for the board the program is executing on.
        /// </summary>
        /// <returns>A driver that works with the board the program is executing on.</returns>
        private static GpioDriver GetBestDriverForBoardOnLinux()
        {
            string[] cpuInfoLines = File.ReadAllLines(CpuInfoPath);
            Regex regex = new Regex(@"Hardware\s*:\s*(.*)");
            foreach (string cpuInfoLine in cpuInfoLines)
            {
                Match match = regex.Match(cpuInfoLine);
                if (match.Success)
                {
                    if (match.Groups.Count > 1)
                    {
                        if (match.Groups[1].Value == RaspberryPiHardware)
                        {
                            return new RaspberryPi3Driver();
                        }

                        // Commenting out as HummingBoard driver is not implemented yet, will be added back after implementation
                        // https://github.com/dotnet/iot/issues/76
                        // if (match.Groups[1].Value == HummingBoardHardware)
                        // {
                        //     return new HummingBoardDriver();
                        // }
                        return UnixDriver.Create();
                    }
                }
            }

            return UnixDriver.Create();
        }

        /// <summary>
        /// Attempt to get the best applicable driver for the board the program is executing on.
        /// </summary>
        /// <returns>A driver that works with the board the program is executing on.</returns>
        /// <remarks>
        ///     This really feels like it needs a driver-based pattern, where each driver exposes a static method:
        ///     public static bool IsSpecificToCurrentEnvironment { get; }
        ///     The GpioController could use reflection to find all GpioDriver-derived classes and call this
        ///     static method to determine if the driver considers itself to be the best match for the environment.
        /// </remarks>
        private static GpioDriver GetBestDriverForBoardOnWindows()
        {
            string baseBoardProduct = Registry.LocalMachine.GetValue(BaseBoardProductRegistryValue, string.Empty).ToString();

            if (baseBoardProduct == RaspberryPi3Product || baseBoardProduct.StartsWith($"{RaspberryPi3Product} ") ||
                baseBoardProduct == RaspberryPi2Product || baseBoardProduct.StartsWith($"{RaspberryPi2Product} "))
            {
                return new RaspberryPi3Driver();
            }

            if (baseBoardProduct == HummingBoardProduct || baseBoardProduct.StartsWith($"{HummingBoardProduct} "))
            {
                return new HummingBoardDriver();
            }

            // Default for Windows IoT Core on a non-specific device
            return new Windows10Driver();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Ili934x
{
    /// <summary>
    /// Binding for Chipsemi CHSC6540 capacitive touch screen controller
    /// Used for instance on the M5Tough in conjunction with an ILI9342 display controller.
    /// Note: The M5Core2, while being very similar to the M5Tough otherwise, has a FT6336U instead.
    /// The two chips are not compatible to each other.
    /// </summary>
    public class Chsc6440 : IDisposable
    {
        private readonly int _interruptPin;
        private readonly bool _shouldDispose;
        private GpioController? _gpioController;
        private I2cDevice _i2c;

        /// <summary>
        /// Create a controller from the given I2C device
        /// </summary>
        /// <param name="device">An I2C device</param>
        /// <param name="interruptPin">The interrupt pin to use, -1 to disable</param>
        /// <param name="gpioController">The gpio controller the interrupt pin is attached to</param>
        /// <param name="shouldDispose">True to dispose the gpio controller on close</param>
        public Chsc6440(I2cDevice device, int interruptPin = -1, GpioController? gpioController = null, bool shouldDispose = true)
        {
            _i2c = device;
            _interruptPin = interruptPin;
            _gpioController = gpioController;
            _shouldDispose = shouldDispose;
            Span<byte> initData = stackalloc byte[2]
            {
                0x5A, 0x5A
            };
            _i2c.Write(initData);
            if (_interruptPin >= 0)
            {
                if (_gpioController == null)
                {
                    throw new ArgumentNullException(nameof(gpioController));
                }

                _gpioController.RegisterCallbackForPinValueChangedEvent(_interruptPin, PinEventTypes.Rising | PinEventTypes.Falling, OnInterrupt);
            }
        }

        private void OnInterrupt(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
        }

        /// <summary>
        /// Returns true if the interrupt pin is set, meaning something is touching the display
        /// </summary>
        /// <returns>True if something presses the display, false if not. Null if no interrupt pin is defined</returns>
        public bool? IsPressed()
        {
            if (_gpioController != null)
            {
                return _gpioController.Read(_interruptPin) == PinValue.Low;
            }

            return null;
        }

        /// <summary>
        /// Dispose of this instance and close connections
        /// </summary>
        public virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_interruptPin >= 0 && _gpioController != null)
                {
                    _gpioController.UnregisterCallbackForPinValueChangedEvent(_interruptPin, OnInterrupt);
                    _gpioController.ClosePin(_interruptPin);

                    if (_shouldDispose)
                    {
                        _gpioController.Dispose();
                    }
                }

                _gpioController = null;

                if (_i2c != null)
                {
                    _i2c.Dispose();
                }

                _i2c = null!;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

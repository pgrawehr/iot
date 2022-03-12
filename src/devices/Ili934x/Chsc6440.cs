using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

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
        /// <summary>
        /// The default I2C address of this chip
        /// </summary>
        public const int DefaultI2cAddress = 0x2E;

        private readonly int _interruptPin;
        private readonly bool _shouldDispose;
        private GpioController? _gpioController;
        private I2cDevice _i2c;

        private bool _wasRead;
        private bool _changed;

        private DateTime _lastRead;
        private TimeSpan _interval;

        private Point[] _points;
        private int _point0finger;
        private int _activeTouches;

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
            _wasRead = false;
            _changed = false;
            _point0finger = 0;
            _points = new Point[2];
            _lastRead = DateTime.MinValue;
            _activeTouches = 0;
            _interval = TimeSpan.FromMilliseconds(20);

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

                _gpioController.OpenPin(_interruptPin, PinMode.Input);
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
        public bool IsPressed()
        {
            ////if (_gpioController != null)
            ////{
            ////    return _gpioController.Read(_interruptPin) == PinValue.High;
            ////}

            // Need to query the device instead
            Span<byte> register = stackalloc byte[1]
            {
                0x02
            };

            Span<byte> result = stackalloc byte[1];

            _i2c.WriteRead(register, result);

            return result[0] != 0;
        }

        private bool ReadData()
        {
            // true if real read, not a "come back later"
            _wasRead = false;

            // true is something actually changed on the touchpad
            _changed = false;

            // Return immediately if read() is called more frequently than the
            // touch sensor updates. This prevents unnecessary I2C reads, and the
            // data can also get corrupted if reads are too close together.
            if (DateTime.UtcNow - _lastRead < _interval)
            {
                return false;
            }

            _lastRead = DateTime.UtcNow;

            Span<Point> p = stackalloc Point[2];
            byte pts = 0;
            int p0f = 0;
            Span<byte> register = stackalloc byte[1]
            {
                0x02
            };

            if (IsPressed())
            {
                Span<byte> data = stackalloc byte[11];
                _i2c.WriteRead(register, data);

                pts = data[0];
                if (pts > 2)
                {
                    return false;
                }

                if (pts > 0)
                {
                    // Read the data. Never mind trying to read the "weight" and
                    // "size" properties or using the built-in gestures: they
                    // are always set to zero.
                    p0f = (data[3] >> 4 != 0) ? 1 : 0;
                    p[0].X = ((data[1] << 8) | data[2]) & 0x0fff;
                    p[0].Y = ((data[3] << 8) | data[4]) & 0x0fff;
                    if (pts == 2)
                    {
                        p[1].X = ((data[7] << 8) | data[8]) & 0x0fff;
                        p[1].Y = ((data[9] << 8) | data[10]) & 0x0fff;
                    }
                }
            }

            if (p[0] != _points[0] || p[1] != _points[1])
            {
                _changed = true;
                _points[0] = p[0];
                _points[1] = p[1];
                _point0finger = p0f;
                _activeTouches = pts;
            }

            _wasRead = true;
            return true;
        }

        /// <summary>
        /// Gets the primary touch point or null if the screen is not being touched
        /// </summary>
        /// <returns>A point where the first finger is</returns>
        public Point? GetPrimaryTouchPoint()
        {
            ReadData();
            if (!_wasRead)
            {
                return null;
            }

            if (_activeTouches >= 1)
            {
                return _points[0];
            }

            return null;
        }

        /// <summary>
        /// Returns true if the data has changed since this method was called last
        /// </summary>
        public bool HasChanged()
        {
            ReadData();
            bool ret = _changed;
            _changed = false;
            return ret;
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

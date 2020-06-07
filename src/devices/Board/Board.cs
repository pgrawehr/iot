using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Pwm;
using System.Device.Spi;
using System.IO;
using System.Text;

namespace Iot.Device.Board
{
    public abstract class Board : MarshalByRefObject, IDisposable
    {
        private readonly PinNumberingScheme _defaultNumberingScheme;
        private readonly object _pinReservationsLock;
        private readonly Dictionary<int, PinReservation> _pinReservations;
        private bool _initialized;
        private bool _disposed;

        protected Board(PinNumberingScheme defaultNumberingScheme)
        {
            _defaultNumberingScheme = defaultNumberingScheme;
            _pinReservations = new Dictionary<int, PinReservation>();
            _pinReservationsLock = new object();
            _initialized = false;
            _disposed = false;
        }

        ~Board()
        {
            Dispose(false);
        }

        public event Action<string, Exception> LogMessages;

        protected bool Initialized
        {
            get
            {
                return _initialized;
            }
        }

        protected bool Disposed
        {
            get
            {
                return _disposed;
            }
        }

        public PinNumberingScheme DefaultPinNumberingScheme
        {
            get
            {
                return _defaultNumberingScheme;
            }
        }

        protected void Log(string message, Exception exception = null)
        {
            LogMessages?.Invoke(message, null);
        }

        /// <summary>
        /// Converts pin numbers in the active <see cref="PinNumberingScheme"/> to logical pin numbers.
        /// Does nothing if <see cref="PinNumberingScheme"/> is logical
        /// </summary>
        /// <param name="pinNumber">Pin numbers</param>
        /// <returns>The logical pin number</returns>
        public abstract int ConvertPinNumberToLogicalNumberingScheme(int pinNumber);

        /// <summary>
        /// Converts logical pin numbers to the active numbering scheme.
        /// This is the opposite of <see cref="ConvertPinNumberToLogicalNumberingScheme"/>.
        /// </summary>
        /// <param name="pinNumber">Logical pin number</param>
        /// <returns>The pin number in the given pin numbering scheme</returns>
        public abstract int ConvertLogicalNumberingSchemeToPinNumber(int pinNumber);

        /// <summary>
        /// Reserves a pin for a specific usage. This is done automatically if a known interface (i.e. GpioController) is
        /// used to open the pin, but may be used to block a pin explicitly, i.e. for UART.
        /// </summary>
        /// <param name="pinNumber">The pin number, in the boards default numbering scheme</param>
        /// <param name="usage">Intended usage of the pin</param>
        /// <param name="owner">Class that owns the pin (use "this")</param>
        public virtual void ReservePin(int pinNumber, PinUsage usage, object owner)
        {
            if (!_initialized)
            {
                Initialize();
            }

            int logicalPin = ConvertPinNumberToLogicalNumberingScheme(pinNumber);
            lock (_pinReservationsLock)
            {
                if (_pinReservations.TryGetValue(logicalPin, out var reservation))
                {
                    throw new InvalidOperationException($"Pin {pinNumber} has already been reserved for {reservation.Usage} by class {reservation.Owner}.");
                }

                PinReservation rsv = new PinReservation(logicalPin, usage, owner);
                _pinReservations.Add(logicalPin, rsv);
            }

            ActivatePinMode(logicalPin, usage);
        }

        public virtual void ReleasePin(int pinNumber, PinUsage usage, object owner)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Cannot release a pin if board is not initialized.");
            }

            int logicalPin = ConvertPinNumberToLogicalNumberingScheme(pinNumber);
            lock (_pinReservationsLock)
            {
                if (_pinReservations.TryGetValue(logicalPin, out var reservation))
                {
                    if (reservation.Owner != owner || reservation.Usage != usage)
                    {
                        throw new InvalidOperationException($"Cannot release Pin {pinNumber}, because you are not the owner or the usage is wrong. Class {reservation.Owner} has reserved the Pin for {reservation.Usage}");
                    }

                    _pinReservations.Remove(logicalPin);
                }
                else
                {
                    throw new InvalidOperationException($"Cannot release Pin {pinNumber}, because it is not reserved.");
                }
            }
        }

        /// <summary>
        /// Override this method if something special needs to be done to use the pin for the given device.
        /// Many devices support multiple functions per Pin, but not at the same time, so that some kind of
        /// multiplexer needs to be set accordingly.
        /// </summary>
        /// <param name="pinNumber">The logical pin number to use.</param>
        /// <param name="usage">The intended usage</param>
        protected virtual void ActivatePinMode(int pinNumber, PinUsage usage)
        {
        }

        public virtual void Initialize()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(ToString());
            }

            _initialized = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private sealed class PinReservation
        {
            public PinReservation(int pin, PinUsage usage, object owner)
            {
                Pin = pin;
                Usage = usage;
                Owner = owner;
            }

            public int Pin { get; }
            public PinUsage Usage { get; }

            /// <summary>
            /// Component that owns the pin (used mainly for debugging)
            /// </summary>
            public object Owner { get; }
        }

        public abstract GpioController CreateGpioController(int[] pinAssignment = null);

        public virtual GpioController CreateGpioController(int[] pinAssignment, PinNumberingScheme pinNumberingScheme)
        {
            return CreateGpioController(RemapPins(pinAssignment, pinNumberingScheme));
        }

        public abstract I2cDevice CreateI2cDevice(I2cConnectionSettings connectionSettings, int[] pinAssignment, PinNumberingScheme pinNumberingScheme);

        public virtual I2cDevice CreateI2cDevice(I2cConnectionSettings connectionSettings)
        {
            // Returns logical pin numbers for the selected bus (or an exception if using a bus number > 1, because that
            // requires specifying the pins)
            int[] pinAssignment = GetPinAssignmentForI2c(connectionSettings, null);
            return CreateI2cDevice(connectionSettings, pinAssignment, PinNumberingScheme.Logical);
        }

        public abstract SpiDevice CreateSpiDevice(SpiConnectionSettings settings);

        public abstract PwmChannel CreatePwmChannel(int chip, int channel, int frequency, double dutyCyclePercentage,
            int pin, PinNumberingScheme pinNumberingScheme);

        public virtual PwmChannel CreatePwmChannel(
            int chip,
            int channel,
            int frequency = 400,
            double dutyCyclePercentage = 0.5)
        {
            int pin = GetPinAssignmentForPwm(chip, channel);
            return CreatePwmChannel(chip, channel, frequency, dutyCyclePercentage, pin, PinNumberingScheme.Logical);
        }

        protected abstract int GetPinAssignmentForPwm(int chip, int channel);

        /// <summary>
        /// Gets the board-specific hardware mode for a particular pin and pin usage (i.e. the different ALTn modes on the raspberry pi)
        /// </summary>
        /// <param name="pinNumber">Pin number to use</param>
        /// <param name="usage">Requested usage</param>
        /// <param name="pinNumberingScheme">Pin numbering scheme for the pin provided (logical or physical)</param>
        /// <param name="bus">Optional bus argument, for SPI and I2C pins</param>
        /// <returns>
        /// -2: It is unknown whether this pin can be used for the given usage
        /// -1: Pin does not support the given usage
        /// 0: Pin supports the given usage, no special mode is needed (i.e. digital in/out)
        /// >0: Mode to set (hardware dependent)</returns>
        public abstract AlternatePinMode GetHardwareModeForPinUsage(int pinNumber, PinUsage usage,
            PinNumberingScheme pinNumberingScheme = PinNumberingScheme.Logical, int bus = 0);

        protected abstract int[] GetPinAssignmentForI2c(I2cConnectionSettings connectionSettings, int[] logicalPinAssignment);

        protected int RemapPin(int pin, PinNumberingScheme providedScheme)
        {
            if (providedScheme == PinNumberingScheme.Logical)
            {
                return pin;
            }

            return ConvertPinNumberToLogicalNumberingScheme(pin);
        }

        protected int[] RemapPins(int[] pins, PinNumberingScheme providedScheme)
        {
            if (pins == null)
            {
                return null;
            }

            if (providedScheme == PinNumberingScheme.Logical)
            {
                return pins;
            }

            int[] newPins = new int[pins.Length];
            for (int i = 0; i < pins.Length; i++)
            {
                newPins[i] = ConvertPinNumberToLogicalNumberingScheme(pins[i]);
            }

            return newPins;
        }

        //// Todo separately
        //// public abstract AnalogController CreateAnalogController(int chip);

        public static Board DetermineOptimalBoardForHardware(PinNumberingScheme defaultNumberingScheme = PinNumberingScheme.Logical)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                Board board = null;
                try
                {
                    board = new RaspberryPiBoard(defaultNumberingScheme);
                    board.Initialize();
                }
                catch (Exception x) when ((x is NotSupportedException) || (x is IOException))
                {
                    board?.Dispose();
                    board = null;
                }

                if (board != null)
                {
                    return board;
                }

                try
                {
                    board = new GenericBoard(defaultNumberingScheme);
                    board.Initialize();
                }
                catch (Exception x) when ((x is NotSupportedException) || (x is IOException))
                {
                    board?.Dispose();
                    board = null;
                }

                if (board != null)
                {
                    return board;
                }
            }
            else
            {
                // TODO: Create WindowsBoard()
            }

            throw new PlatformNotSupportedException("Could not find a matching board driver for this hardware");
        }
    }
}

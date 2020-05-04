using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using System.Threading;
using Iot.Device.Mcp23xxx;

namespace DisplayControl
{
    /// <summary>
    /// Engine control using an MCP23017 and a CD4510B counter
    /// </summary>
    public class EngineSurveillance : PollingSensorBase
    {
        private const int InterruptPin = 21;
        private static readonly TimeSpan MaxIdleTime = TimeSpan.FromSeconds(8);
        private const double TicksPerRevolution = 1.0;

        private enum PinUsage
        {
            P1 = 0,
            P2 = 1,
            P3 = 2,
            P4 = 3,
            UpDown = 4,
            Reset = 5,
            CarryIn = 6, // Into the CD4510B
            Q1 = 8,
            Q2 = 9,
            Q3 = 10,
            Q4 = 11,
            CarryOut = 12, // Out from the CD4510B
            PresetEnable = 15,
        }

        private I2cDevice _device;
        private Mcp23017Ex _mcp23017;
        private GpioController _controllerUsingMcp;
        private ObservableValue<int> _rpm;
        private ObservableValue<bool> _engineOn;
        private DateTimeOffset _lastInterrupt;
        private ObservableValue<TimeSpan> _engineOperatingHours;

        private int _lastCounterValue;
        private int _totalCounterValue;

        private object _counterLock;

        /// <summary>
        /// Create an instance of this class.
        /// Note: Adapt polling timeout when needed
        /// </summary>
        public EngineSurveillance() 
            : base(TimeSpan.FromSeconds(5))
        {
            _counterLock = new object();
        }

        public GpioController MainController
        {
            get;
            private set;
        }

        public override void Init(GpioController gpioController)
        {
            MainController = gpioController;
            _device = I2cDevice.Create(new I2cConnectionSettings(1, 0x21));
            // Interrupt pin B is connected to GPIO pin 22
            _mcp23017 = new Mcp23017Ex(_device, -1, -1, InterruptPin, gpioController, false);
            _controllerUsingMcp = new GpioController(PinNumberingScheme.Logical, _mcp23017);

            _engineOn = new ObservableValue<bool>("Motor läuft", string.Empty, false);
            _rpm = new ObservableValue<int>("Motordrehzahl", "U/Min", 0);
            _engineOperatingHours = new ObservableValue<TimeSpan>("Motorstunden", "h");
            _totalCounterValue = 0;

            // Just open all the pins
            for (int i = 0; i < _controllerUsingMcp.PinCount; i++)
            {
                _controllerUsingMcp.OpenPin(i);
            }

            SensorValueSources.Add(_engineOn);
            SensorValueSources.Add(_rpm);
            SensorValueSources.Add(_engineOperatingHours);

            bool initialCarryIn = false;
            _controllerUsingMcp.SetPinMode((int)PinUsage.Reset, PinMode.Output);
            Write(PinUsage.Reset, PinValue.Low);
            _controllerUsingMcp.SetPinMode((int)PinUsage.Q1, PinMode.Input);
            _controllerUsingMcp.SetPinMode((int)PinUsage.Q2, PinMode.Input);
            _controllerUsingMcp.SetPinMode((int)PinUsage.Q3, PinMode.Input);
            _controllerUsingMcp.SetPinMode((int)PinUsage.Q4, PinMode.Input);
            _controllerUsingMcp.SetPinMode((int)PinUsage.CarryOut, PinMode.Input);
            _controllerUsingMcp.SetPinMode((int)PinUsage.PresetEnable, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.CarryIn, PinMode.Output);
            Write(PinUsage.CarryIn, initialCarryIn); // This pin is inverted (?)
            Write(PinUsage.PresetEnable, PinValue.Low);
            _controllerUsingMcp.SetPinMode((int)PinUsage.P1, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.P2, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.P3, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.P4, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.UpDown, PinMode.Output);
            // Set the initial value to something other than 0, so we are sure we're not stuck in reset mode
            int initialValue = 0;
            // Run trough all possible values, to check all bit lines are working
            for (initialValue = 10; initialValue >= 0; initialValue--)
            {
                Write(PinUsage.P1, initialValue & 0x1);
                Write(PinUsage.P2, initialValue & 0x2);
                Write(PinUsage.P3, initialValue & 0x4);
                Write(PinUsage.P4, initialValue & 0x8);
                Write(PinUsage.UpDown, PinValue.High); // Count up. TODO: Check this is right (it may not properly roll over)

                // Reset to 0
                Write(PinUsage.PresetEnable, PinValue.High);
                Thread.Sleep(1);
                Write(PinUsage.PresetEnable, PinValue.Low);

                // Set all preset bits 0, to make sure there isn't a short between Ps and Qs (did occur to me - nasty to find)
                Write(PinUsage.P1, 0);
                Write(PinUsage.P2, 0);
                Write(PinUsage.P3, 0);
                Write(PinUsage.P4, 0);

                int counter = ReadCurrentCounterValue();
                if (counter != initialValue)
                {
                    throw new InvalidOperationException($"Engine revolution counter: Bit error (should be {initialValue} but was {counter}.)");
                }
            }

            _lastCounterValue = 0;
            _lastInterrupt = DateTimeOffset.UtcNow;
            // Enable interrupt if Q1 (the lowest bit of the counter) changes
            _mcp23017.EnableInterruptOnChange((int)PinUsage.Q1, PinEventTypes.Rising | PinEventTypes.Falling);
            _mcp23017.EnableInterruptOnChange((int)PinUsage.Q2, PinEventTypes.Rising | PinEventTypes.Falling);
            _mcp23017.EnableInterruptOnChange((int)PinUsage.Q3, PinEventTypes.Rising | PinEventTypes.Falling);
            _mcp23017.EnableInterruptOnChange((int)PinUsage.Q4, PinEventTypes.Rising | PinEventTypes.Falling);
            // The interrupt is signaled with a falling edge
            gpioController.RegisterCallbackForPinValueChangedEvent(InterruptPin, PinEventTypes.Falling, Interrupt);
            // To ensure the interrupt register is reset (if we missed a previous interrupt, a new one would never trigger)
            ReadCurrentCounterValue(); 
            base.Init(gpioController);
        }

        private void Interrupt(object sender, PinValueChangedEventArgs pinvaluechangedeventargs)
        {
            // We don't really care what caused the interrupt to trigger - we just read if we receive any interrupt.
            lock (_counterLock)
            {
                int newValue = ReadCurrentCounterValue();
                DateTimeOffset now = DateTimeOffset.UtcNow;
                int ticksSinceLast = newValue - _lastCounterValue;
                if (ticksSinceLast < 0)
                {
                    // Our counter chip is a BCD counter, so it's maximum value is 10, consider this in a wrap around case
                    ticksSinceLast = 10 - _lastCounterValue + newValue;
                }

                _totalCounterValue += ticksSinceLast;
                TimeSpan timeSinceLastInterrupt = now - _lastInterrupt;
                // This should probably be calculated using a filter. Now it just calculates the revs/minute on every tick
                double umin = (ticksSinceLast / TicksPerRevolution) / timeSinceLastInterrupt.TotalMinutes;

                if (_engineOn.Value)
                {
                    // The engine ran during this tick, only if previously it ran already
                    _engineOperatingHours.Value += timeSinceLastInterrupt;
                }

                _rpm.Value = (int)umin;
                _engineOn.Value = true;

                _lastCounterValue = newValue;
                _lastInterrupt = now;
            }
        }

        private void Write(PinUsage pin, in PinValue value)
        {
            _controllerUsingMcp.Write((int)pin, value);
        }

        private int ReadCurrentCounterValue()
        {
            return _mcp23017.ReadPortB() & 0x0F;
        }

        protected override void UpdateSensors()
        {
            TimeSpan timeSinceLastInterrupt = DateTimeOffset.UtcNow - _lastInterrupt;
            lock (_counterLock)
            {
                int value = ReadCurrentCounterValue();
                if (value != _lastCounterValue)
                {
                    // We probably lost an interrupt. The above call will reset the interrupt pending flag and
                    // we'll resync with the next interrupt.
                    return;
                }
                if (timeSinceLastInterrupt > MaxIdleTime)
                {
                    _engineOn.Value = false;
                    _rpm.Value = 0;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            _mcp23017.DisableInterruptOnChange((int)PinUsage.Q1);
            _mcp23017.DisableInterruptOnChange((int)PinUsage.Q2);
            _mcp23017.DisableInterruptOnChange((int)PinUsage.Q3);
            _mcp23017.DisableInterruptOnChange((int)PinUsage.Q4);
            MainController.UnregisterCallbackForPinValueChangedEvent(InterruptPin, Interrupt);
            _controllerUsingMcp.Dispose();
            _mcp23017.Dispose();
            base.Dispose(disposing);
        }
    }
}

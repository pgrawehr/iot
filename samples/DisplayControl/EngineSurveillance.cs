using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Linq;
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
        private static readonly TimeSpan AveragingTime = TimeSpan.FromSeconds(5);
        private const double TicksPerRevolution = 1.0;
        private int _maxCounterValue;
        private Queue<CounterEvent> _lastEvents;

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
        private long _lastInterrupt;
        private ObservableValue<TimeSpan> _engineOperatingHours;

        private int _lastCounterValue;
        private int _totalCounterValue;

        private object _counterLock;

        /// <summary>
        /// Create an instance of this class.
        /// Note: Adapt polling timeout when needed
        /// </summary>
        /// <param name="maxCounterValue">The maximum value of the counter. 9 for a BCD type counter, 15 for a binary counter</param>
        public EngineSurveillance(int maxCounterValue)
            : base(TimeSpan.FromSeconds(5))
        {
            _counterLock = new object();
            _maxCounterValue = maxCounterValue;
            _lastEvents = new Queue<CounterEvent>();
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

            _controllerUsingMcp.SetPinMode((int)PinUsage.Reset, PinMode.Output);
            Write(PinUsage.Reset, PinValue.Low);
            _controllerUsingMcp.SetPinMode((int)PinUsage.Q1, PinMode.Input);
            _controllerUsingMcp.SetPinMode((int)PinUsage.Q2, PinMode.Input);
            _controllerUsingMcp.SetPinMode((int)PinUsage.Q3, PinMode.Input);
            _controllerUsingMcp.SetPinMode((int)PinUsage.Q4, PinMode.Input);
            _controllerUsingMcp.SetPinMode((int)PinUsage.CarryOut, PinMode.Input);
            _controllerUsingMcp.SetPinMode((int)PinUsage.PresetEnable, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.CarryIn, PinMode.Output);
            Write(PinUsage.CarryIn, false);
            Write(PinUsage.PresetEnable, PinValue.Low);
            _controllerUsingMcp.SetPinMode((int)PinUsage.P1, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.P2, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.P3, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.P4, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.UpDown, PinMode.Output);
            // Set the initial value to something other than 0, so we are sure we're not stuck in reset mode
            int initialValue = 0;
            // Run trough all possible values, to check all bit lines are working
            for (initialValue = _maxCounterValue; initialValue >= 0; initialValue--)
            {
                Write(PinUsage.P1, initialValue & 0x1);
                Write(PinUsage.P2, initialValue & 0x2);
                Write(PinUsage.P3, initialValue & 0x4);
                Write(PinUsage.P4, initialValue & 0x8);
                Write(PinUsage.UpDown, PinValue.High); // Count up.

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
            _lastInterrupt = Environment.TickCount64;
            // Enable interrupt if any of the Q pins change
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
                long now = Environment.TickCount64;
                int ticksSinceLast = newValue - _lastCounterValue;
                if (ticksSinceLast < 0)
                {
                    // Check the possible max value for the wrap around case
                    ticksSinceLast = _maxCounterValue + 1 - _lastCounterValue + newValue;
                }
                else if (ticksSinceLast == 0)
                {
                    return;
                }
                
                _totalCounterValue += ticksSinceLast;
                var ce = new CounterEvent(now, _totalCounterValue);
                _lastEvents.Enqueue(ce);

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
            long now = Environment.TickCount64;
            List<CounterEvent> eventsToObserve = null;
            lock (_counterLock)
            {
                // In case we lost one interrupt, we poll occasionally to reset the interrupt flag
                Interrupt(this, new PinValueChangedEventArgs(PinEventTypes.None, InterruptPin));
                long removeOlderThan = now - (long)Math.Max(AveragingTime.TotalMilliseconds, MaxIdleTime.TotalMilliseconds);
                while (_lastEvents.TryPeek(out var result) && result.TickCount < removeOlderThan)
                {
                    _lastEvents.Dequeue();
                }

                eventsToObserve = _lastEvents.OrderBy(x => x.TickCount).ToList();
            }

            if (eventsToObserve.Count == 0)
            {
                _engineOn.Value = false;
            }
            else
            {
                _engineOn.Value = true;
            }

            double umin = 0;
            long oldestToInspect = now - (long)AveragingTime.TotalMilliseconds;
            var firstEventInTimeFrame = eventsToObserve.FirstOrDefault(x => x.TickCount >= oldestToInspect);
            var lastEventInTimeFrame = eventsToObserve.LastOrDefault();
            if (firstEventInTimeFrame != null && lastEventInTimeFrame != firstEventInTimeFrame)
            {
                // This cannot be null here (because if first is not null, last can't be)
                long deltaTime = lastEventInTimeFrame.TickCount - firstEventInTimeFrame.TickCount;
                int revolutions = lastEventInTimeFrame.TotalCounter - firstEventInTimeFrame.TotalCounter;
                if (deltaTime > 0)
                {
                    // revs per ms
                    umin = (revolutions / TicksPerRevolution) / deltaTime;
                    // revs per minute
                    umin = umin * 1000 * 60;
                }
            }

            _rpm.Value = (int)umin;
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

        private class CounterEvent
        {
            public CounterEvent(long tickCount, int totalCounter)
            {
                TickCount = tickCount;
                TotalCounter = totalCounter;
            }

            public long TickCount { get; }
            public int TotalCounter { get; }
        }
    }
}

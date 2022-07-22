using System;
using System.Collections.Generic;
using System.Data;
using System.Device.Gpio;
using System.Device.I2c;
using System.Linq;
using System.Text;
using System.Threading;
using Iot.Device.Mcp23xxx;
using Iot.Device.Common;
using Iot.Device.Nmea0183.Sentences;
using Microsoft.Extensions.Logging;
using UnitsNet;
using UnitsNet.Units;

namespace DisplayControl
{
    /// <summary>
    /// Engine control using an MCP23017 and a CD4510B counter
    /// </summary>
    public class EngineSurveillance : PollingSensorBase
    {
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

        private const int InterruptPin = 21;
        private static readonly TimeSpan MaxIdleTime = TimeSpan.FromSeconds(8);
        private static readonly TimeSpan AveragingTime = TimeSpan.FromSeconds(3);
        private int _maxCounterValue;
        private Queue<CounterEvent> _lastEvents;
        private bool _engineOn;
        private PersistentTimeSpan _engineOperatingTime;
        private PersistentTimeSpan _engineOperatingTimeAtLastRefill;
        private PersistentDouble _engineRpmFactor;
        private long _lastTickForUpdate;
        private PersistenceFile _enginePersistenceFile;
        private I2cDevice _device;
        private Mcp23017Ex _mcp23017;
        private GpioController _controllerUsingMcp;
        private double _rpm;
        private bool _inSelfTest;

        private int _lastCounterValue;
        private int _totalCounterValue;

        public SensorMeasurement Engine0OperatingTimeSinceRefill = new SensorMeasurement("Engine 0 operating time since refill", Duration.Zero, SensorSource.Engine, 0);

        private object _counterLock;
        private ILogger _logger;

        /// <summary>
        /// Create an instance of this class.
        /// Note: Adapt polling timeout when needed
        /// </summary>
        /// <param name="manager">Measurement manager</param>
        /// <param name="maxCounterValue">The maximum value of the counter. 9 for a BCD type counter, 15 for a binary counter</param>
        public EngineSurveillance(MeasurementManager manager, int maxCounterValue)
            : base(manager, TimeSpan.FromSeconds(1))
        {
            _counterLock = new object();
            _maxCounterValue = maxCounterValue;
            _lastEvents = new Queue<CounterEvent>();
            _engineOn = false;
            _lastTickForUpdate = 0;
            _rpm = 0;
            _inSelfTest = false;
            _logger = this.GetCurrentClassLogger();
            _enginePersistenceFile = new PersistenceFile("/home/pi/projects/ShipLogs/Engine.txt");
            _engineOperatingTime = new PersistentTimeSpan(_enginePersistenceFile, "Operating Hours", TimeSpan.Zero, TimeSpan.FromMinutes(1));
            _engineOperatingTimeAtLastRefill = new PersistentTimeSpan(_enginePersistenceFile, "Operating Hours at last refill", new TimeSpan(0, 15, 33, 0), TimeSpan.Zero);
            _engineRpmFactor = new PersistentDouble(_enginePersistenceFile, "Engine RPM correction factor", 1.87, TimeSpan.Zero);
        }

        // Todo: Better interface (maybe some generic data provider interface)
        public event Action<EngineData> DataChanged;

        public GpioController MainController
        {
            get;
            private set;
        }

        public double EngineRpmCorrectionFactor
        {
            get
            {
                return _engineRpmFactor.Value;
            }
        }

        public override void Init(GpioController gpioController)
        {
            _inSelfTest = true;
            MainController = gpioController;
            _device = I2cDevice.Create(new I2cConnectionSettings(1, 0x21));
            // Interrupt pin B is connected to GPIO pin 22
            _mcp23017 = new Mcp23017Ex(_device, -1, -1, InterruptPin, gpioController, false);
            _controllerUsingMcp = new GpioController(PinNumberingScheme.Logical, _mcp23017);

            Manager.AddRange(new []
            {
                SensorMeasurement.Engine0On, SensorMeasurement.Engine0Rpm, SensorMeasurement.Engine0OperatingTime, 
                Engine0OperatingTimeSinceRefill,
            });

            _totalCounterValue = 0;

            // Just open all the pins
            for (int i = 0; i < _controllerUsingMcp.PinCount; i++)
            {
                _controllerUsingMcp.OpenPin(i);
            }

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
            bool alreadyRunning = false;
            // Run trough all possible values, to check all bit lines are working
            for (initialValue = _maxCounterValue; initialValue >= 0; initialValue--)
            {
                WritePresetValue(initialValue);

                // Set all preset bits 0, to make sure there isn't a short between Ps and Qs (did occur to me - nasty to find)
                Write(PinUsage.P1, 0);
                Write(PinUsage.P2, 0);
                Write(PinUsage.P3, 0);
                Write(PinUsage.P4, 0);

                int counter = ReadCurrentCounterValue();
                if (counter != initialValue)
                {
                    string msg = $"Engine revolution counter: Bit error (should be {initialValue} but was {counter}.)";
                    _logger.LogError(msg);
                    // Print an obvious message (but don't fail initialization if we have to restart while the engine is running)
                    for (int i = 0; i < 50; i++)
                    {
                        _logger.LogError("Is the engine running??");
                        Thread.Sleep(50);
                        alreadyRunning = true;
                    }
                }
            }

            _lastCounterValue = 0;
            // Enable interrupt if any of the Q pins change
            _mcp23017.EnableInterruptOnChange((int)PinUsage.Q1, PinEventTypes.Rising | PinEventTypes.Falling);
            _mcp23017.EnableInterruptOnChange((int)PinUsage.Q2, PinEventTypes.Rising | PinEventTypes.Falling);
            _mcp23017.EnableInterruptOnChange((int)PinUsage.Q3, PinEventTypes.Rising | PinEventTypes.Falling);
            _mcp23017.EnableInterruptOnChange((int)PinUsage.Q4, PinEventTypes.Rising | PinEventTypes.Falling);
            // The interrupt is signaled with a falling edge
            gpioController.RegisterCallbackForPinValueChangedEvent(InterruptPin, PinEventTypes.Falling, Interrupt);
            // To ensure the interrupt register is reset (if we missed a previous interrupt, a new one would never trigger)
            ReadCurrentCounterValue();
            _lastTickForUpdate = Environment.TickCount64;
            if (alreadyRunning)
            {
                PerformExtendedSelfTests();
            }

            _lastTickForUpdate = Environment.TickCount64;
            base.Init(gpioController);
            lock (_counterLock)
            {
                _lastEvents.Clear();
            }
            _logger.LogInformation("Engine controller self test completed.");
            _inSelfTest = false;
        }

        private void WritePresetValue(int initialValue)
        {
            Write(PinUsage.P1, initialValue & 0x1);
            Write(PinUsage.P2, initialValue & 0x2);
            Write(PinUsage.P3, initialValue & 0x4);
            Write(PinUsage.P4, initialValue & 0x8);
            Write(PinUsage.UpDown, PinValue.High); // Count up.

            // Pulse the PresetEnable line
            Write(PinUsage.PresetEnable, PinValue.High);
            Thread.Sleep(1);
            Write(PinUsage.PresetEnable, PinValue.Low);
        }

        private void SimulateSteps(int noSteps)
        {
            int valueToWrite = (_lastCounterValue + noSteps) % (_maxCounterValue + 1);
            WritePresetValue(valueToWrite);
            Interrupt(this, new PinValueChangedEventArgs(PinEventTypes.Falling, InterruptPin));
        }

        private void PerformExtendedSelfTests()
        {
            // Note: while this runs, the update thread is not running yet
            var timePrev = _engineOperatingTime;
            _engineOperatingTime = new PersistentTimeSpan(null, "Operating Hours", TimeSpan.Zero, TimeSpan.FromMinutes(1));

            Check(_engineOn == false);
            // This simulates 100 revolutions per second, that is 6000 per minute
            for (int i = 0; i < 100; i++)
            {
                SimulateSteps(1);
                Thread.Sleep(1);
            }

            UpdateSensors();
            Check(_engineOn == true);
            Check(_engineOperatingTime.Value > TimeSpan.Zero);
            Check(_rpm > 0);
            // When in debug mode, this may be quite small
            Check(_rpm > 1000 && _rpm < 7000);
            _engineOn = false;
            _rpm = 0;
            Check(_engineOn == false);
            _engineOperatingTime = timePrev;
        }

        private void Check(bool condition)
        {
            if (!condition)
            {
                _logger.LogError("Error: Extended self test validation failed");
                throw new InvalidOperationException("Engine counter validation failure");
            }
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
                _engineOn = false;
            }
            else
            {
                _engineOn = true;
                long elapsedSinceLastUpdate = now - _lastTickForUpdate;
                _engineOperatingTime.Value += TimeSpan.FromMilliseconds(elapsedSinceLastUpdate);
            }

            double umin = 0;
            long oldestToInspect = now - (long)AveragingTime.TotalMilliseconds;
            var firstEventInTimeFrame = eventsToObserve.FirstOrDefault(x => x.TickCount >= oldestToInspect);
            var lastEventInTimeFrame = eventsToObserve.LastOrDefault();
            long deltaTime = 0;
            double revolutions = 0;
            if (firstEventInTimeFrame != null && lastEventInTimeFrame != firstEventInTimeFrame)
            {
                // This cannot be null here (because if first is not null, last can't be)
                deltaTime = lastEventInTimeFrame!.TickCount - firstEventInTimeFrame.TickCount;
                revolutions = lastEventInTimeFrame.TotalCounter - firstEventInTimeFrame.TotalCounter;
                if (deltaTime > 0)
                {
                    // revs per ms
                    umin = (revolutions / _engineRpmFactor.Value) / deltaTime;
                    // revs per minute
                    umin = umin * 1000 * 60;
                }
            }

            _rpm = umin;
            _lastTickForUpdate = now;

            if (_engineOn)
            {
                _logger.LogInformation($"Engine status: On. {umin} U/Min, recent event count: {eventsToObserve.Count}. Tick delta: {deltaTime}, Rev delta: {revolutions}");
            }
            // Final step: Send values to UI
            if (!_inSelfTest)
            {
                Manager.UpdateValue(SensorMeasurement.Engine0Rpm, RotationalSpeed.FromRevolutionsPerMinute(_rpm));
                Manager.UpdateValue(SensorMeasurement.Engine0On, _engineOn);
                TimeSpan timeSinceRefill = _engineOperatingTime.Value - _engineOperatingTimeAtLastRefill.Value;
                Manager.UpdateValues(new[] { SensorMeasurement.Engine0OperatingTime, Engine0OperatingTimeSinceRefill },
                    new IQuantity[]
                    {
                        Duration.FromSeconds(_engineOperatingTime.Value.TotalSeconds).ToUnit(DurationUnit.Hour),
                        Duration.FromSeconds(timeSinceRefill.TotalSeconds).ToUnit(DurationUnit.Hour)
                    });
            }

            Temperature engineTemp;

            if (!SensorMeasurement.Engine0Temperature.TryGetAs(out engineTemp))
            {
                // Obviously wrong value
                engineTemp = Temperature.FromKelvins(0);
            }

            var msg = new EngineData((int)now, 0, RotationalSpeed.FromRevolutionsPerMinute(umin), Ratio.FromPercent(100),
                _engineOperatingTime.Value, engineTemp); // Pitch unknown so far
            DataChanged?.Invoke(msg);
        }

        protected override void Dispose(bool disposing)
        {
            _mcp23017.DisableInterruptOnChange((int)PinUsage.Q1);
            _mcp23017.DisableInterruptOnChange((int)PinUsage.Q2);
            _mcp23017.DisableInterruptOnChange((int)PinUsage.Q3);
            _mcp23017.DisableInterruptOnChange((int)PinUsage.Q4);
            _engineOperatingTime?.Dispose();
            _engineOperatingTimeAtLastRefill?.Dispose();
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

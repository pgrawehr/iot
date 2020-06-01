using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO;
using System.Text;
using System.Threading;
using Iot.Device.CpuTemperature;

namespace DisplayControl
{
    public abstract class PollingSensorBase : IDisposable
    {
        private readonly List<SensorValueSource> _sensorValueSources;

        private Thread _pollThread;
        private CancellationTokenSource _cancellationTokenSource;
        private TimeSpan _pollTime;

        protected PollingSensorBase(TimeSpan pollTime)
        {
            if (pollTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("Poll time must not be negative");
            }
            PollTime = pollTime;
            _sensorValueSources = new List<SensorValueSource>();
        }

        public List<SensorValueSource> SensorValueSources => _sensorValueSources;

        public TimeSpan PollTime
        {
            get
            {
                return _pollTime;
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("Poll time must not be negative");
                }
                _pollTime = value;
            }
        }

        public virtual void Init(GpioController gpioController)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _pollThread = new Thread(PollThread);
            _pollThread.Name = GetType().Name;
            _pollThread.IsBackground = true;
            _pollThread.Start();
        }

        /// <summary>
        /// Use some polling for the sensor values, as this sensor does not support interrupts. 
        /// It needs a low poll rate only, though (can query at most once every 2 secs)
        /// </summary>
        public void PollThread()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    UpdateSensors();
                }
                catch (IOException x)
                {
                    // Likely an I2C communication error, try again
                    Console.WriteLine($"{GetType().Name}: {x}");
                }

                _cancellationTokenSource.Token.WaitHandle.WaitOne(PollTime);
            }
        }

        protected abstract void UpdateSensors();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopThread();
            }
        }

        protected void StopThread()
        {
            if (_pollThread != null)
            {
                _cancellationTokenSource.Cancel();
                _pollThread.Join();
                _cancellationTokenSource.Dispose();
                _pollThread = null;
                _cancellationTokenSource = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

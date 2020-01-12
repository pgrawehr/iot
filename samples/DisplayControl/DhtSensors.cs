using Iot.Device.Ads1115;
using Iot.Device.DHTxx;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using System.Threading;

namespace DisplayControl
{
    public sealed class DhtSensors : IDisposable
    {
        private readonly List<SensorValueSource> _sensorValueSources;

        private Dht11 _dht11;

        private Thread _pollThread;
        private CancellationTokenSource _cancellationTokenSource;

        private ObservableValue<double> _insideTemperature;
        private ObservableValue<double> _insideHumidity;

        public DhtSensors()
        {
            _sensorValueSources = new List<SensorValueSource>();
        }
        
        public IList<SensorValueSource> SensorValueSources => _sensorValueSources;

        public void Init(GpioController gpioController)
        {
            _insideTemperature = new ObservableValue<double>("Inside temperature", "°C", double.NaN);
            // This sensor can deliver an accuracy of 0.1° at most
            _insideTemperature.ValueFormatter = "{0:F1}";
            _sensorValueSources.Add(_insideTemperature);
            _insideHumidity = new ObservableValue<double>("Inside rel humidity", "%", double.NaN);
            // This sensor can deliver an accuracy of 0.1% at most
            _insideHumidity.ValueFormatter = "{0:F1}";
            _sensorValueSources.Add(_insideHumidity);
            _dht11 = new Dht11(gpioController, 16);

            _cancellationTokenSource = new CancellationTokenSource();
            _pollThread = new Thread(PollThread);
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
                var temp = _dht11.Temperature;
                if (_dht11.IsLastReadSuccessful)
                {
                    _insideTemperature.Value = temp.Celsius;
                    _insideHumidity.Value = _dht11.Humidity;
                }
                _cancellationTokenSource.Token.WaitHandle.WaitOne(3000);
            }
        }

        public void Dispose()
        {
            if (_pollThread != null)
            {
                _cancellationTokenSource.Cancel();
                _pollThread.Join();
                _cancellationTokenSource.Dispose();
                _pollThread = null;
                _cancellationTokenSource = null;
            }

            if (_dht11 != null)
            {
                _dht11.Dispose();
                _dht11 = null;
            }
        }
    }
}

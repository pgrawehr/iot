using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO;
using System.IO.Ports;
using System.Numerics;
using System.Text;
using Iot.Device.Imu;
using Iot.Units;

namespace DisplayControl
{
    /// <summary>
    /// This class uses polling only for GUI update
    /// </summary>
    internal class ImuSensor : PollingSensorBase
    {
        public const string ShipPitch = "Ship Pitch";
        public const string ShipRoll = "Ship Roll";
        public const string ShipMagneticHeading = "Ship Mag Heading";
        private Ig500Sensor _imu;
        private SerialPort _serialPort;
        private ObservableValue<double> _pitch;
        private ObservableValue<double> _roll;
        private ObservableValue<double> _heading;
        private ObservableValue<double> _imuTemperature;

        private Vector3 _lastEulerAngles;

        public event Action<Vector3> OnNewOrientation;

        public ImuSensor() : base(TimeSpan.FromSeconds(1))
        {
            _lastEulerAngles = new Vector3();
        }

        public override void Init(GpioController gpioController)
        {
            _pitch = new ObservableValue<double>(ShipPitch, "°", 0);
            _pitch.ValueFormatter = "{0:F1}";
            _roll = new ObservableValue<double>(ShipRoll, "°", 0);
            _roll.ValueFormatter = "{0:F1}";
            _heading = new ObservableValue<double>(ShipMagneticHeading, "°M", 0);
            _heading.ValueFormatter = "{0:F1}";
            _imuTemperature = new ObservableValue<double>("IMU Temperature", "°C", -273);
            _imuTemperature.ValueFormatter = "{0:F1}";

            _serialPort = new SerialPort("/dev/ttyUSB1", 115200, Parity.None);
            _serialPort.Open();
            Ig500Sensor imu = new Ig500Sensor(_serialPort.BaseStream, OutputDataSets.Euler | OutputDataSets.Magnetometers | OutputDataSets.Quaternion |
                                                                      OutputDataSets.Temperatures |
                                                                      OutputDataSets.Accelerometers | OutputDataSets.Gyroscopes);

            if (!imu.WaitForSensorReady(out var errorMessage, TimeSpan.FromSeconds(5)))
            {
                Console.WriteLine($"Error initializing device: {errorMessage}");
                var errors = imu.RecentParserErrors;
                foreach (string error in errors)
                {
                    Console.WriteLine(error);
                }

                throw new IOException($"Error initializing IMU {errorMessage}");
            }

            _imu = imu;
            imu.OnNewData += ImuOnOnNewData;

            SensorValueSources.Add(_pitch);
            SensorValueSources.Add(_roll);
            SensorValueSources.Add(_heading);
            SensorValueSources.Add(_imuTemperature);
            base.Init(gpioController);
        }

        private void ImuOnOnNewData(Vector3 eulerAngles)
        {
            _lastEulerAngles = eulerAngles;
            OnNewOrientation?.Invoke(eulerAngles);
        }

        protected override void UpdateSensors()
        {
            _pitch.Value = _lastEulerAngles.Z;
            _roll.Value = _lastEulerAngles.Y;
            _heading.Value = _lastEulerAngles.X;
            _imuTemperature.Value = _imu.Temperature.Celsius;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_imu != null)
                {
                    _imu.OnNewData -= ImuOnOnNewData;
                    _imu.Dispose();
                    _serialPort.Dispose();
                    _imu = null;
                    _serialPort = null;
                }

                SensorValueSources.Clear();
            }

            base.Dispose(disposing);
        }
    }
}

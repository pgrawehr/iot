using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO;
using System.IO.Ports;
using System.Numerics;
using System.Text;
using Iot.Device.Common;
using Iot.Device.Imu;
using Iot.Device.Nmea0183;
using Iot.Device.Persistence;
using UnitsNet;

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

        private readonly PersistentBool _correctionEnabled;
        private Ig500Sensor _imu;
        private SerialPort _serialPort;
        private ObservableValue<double> _pitch;
        private ObservableValue<double> _roll;
        private ObservableValue<double> _headingUncorrected;
        private ObservableValue<double> _heading;
        private ObservableValue<double> _imuTemperature;

        private Vector3 _lastEulerAngles;
        private MagneticDeviationCorrection _deviationCorrection;

        public event Action<Vector3> OnNewOrientation;

        public ImuSensor(PersistenceFile file) : base(TimeSpan.FromSeconds(1))
        {
            _lastEulerAngles = new Vector3();
            _correctionEnabled = new PersistentBool(file, "DeviationCorrectionEnabled", true);
        }

        public bool DeviationCorrectionEnabled
        {
            get
            {
                return _correctionEnabled.Value;
            }
            set
            {
                _correctionEnabled.Value = value;
            }
        }

        public override void Init(GpioController gpioController)
        {
            _pitch = new ObservableValue<double>(ShipPitch, "°", 0);
            _pitch.ValueFormatter = "{0:F1}";
            _roll = new ObservableValue<double>(ShipRoll, "°", 0);
            _roll.ValueFormatter = "{0:F1}";
            _heading = new ObservableValue<double>(ShipMagneticHeading, "°M", 0);
            _heading.ValueFormatter = "{0:F1}";
            _headingUncorrected = new ObservableValue<double>("Magnetic Compass reading", "°", 0);
            _headingUncorrected.ValueFormatter = "{0:F1}";
            _imuTemperature = new ObservableValue<double>("IMU Temperature", "°C", -273);
            _imuTemperature.ValueFormatter = "{0:F1}";

            _deviationCorrection = new MagneticDeviationCorrection();
            _deviationCorrection.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Calibration_Cirrus.xml"));

            bool success = false;
            string errorMessage = string.Empty;
            Ig500Sensor imu = null;
            for (int i = 0; i <= 4; i++)
            {
                string port = "/dev/ttyUSB" + i;
                _serialPort = new SerialPort(port, 115200, Parity.None);
                try
                {
                    _serialPort.Open();
                }
                catch (IOException x)
                {
                    Console.WriteLine($"Warning: Could not open {port}: {x.Message}");
                    _serialPort.Dispose();
                    _serialPort = null;
                    continue;
                }

                imu = new Ig500Sensor(_serialPort.BaseStream,
                    OutputDataSets.Euler | OutputDataSets.Magnetometers | OutputDataSets.Quaternion |
                    OutputDataSets.Temperatures |
                    OutputDataSets.Accelerometers | OutputDataSets.Gyroscopes);

                if (!imu.WaitForSensorReady(out errorMessage, TimeSpan.FromSeconds(5)))
                {
                    Console.WriteLine($"Error initializing device: {errorMessage}");
                    var errors = imu.RecentParserErrors;
                    foreach (string error in errors)
                    {
                        Console.WriteLine(error);
                    }
                    imu.Dispose();
                    imu = null;
                    _serialPort.Dispose();
                    _serialPort = null;
                }
                else
                {
                    Console.WriteLine($"Found and initialized IMU on {port}.");
                    success = true;
                    break;
                }
            }

            if (!success)
            {
                throw new IOException($"Error initializing IMU {errorMessage}");
            }

            _imu = imu;
            imu.OnNewData += ImuOnNewData;

            SensorValueSources.Add(_pitch);
            SensorValueSources.Add(_roll);
            SensorValueSources.Add(_heading);
            SensorValueSources.Add(_headingUncorrected);
            SensorValueSources.Add(_imuTemperature);

            base.Init(gpioController);
        }

        private void ImuOnNewData(Vector3 eulerAngles)
        {
            _lastEulerAngles = eulerAngles;
            Angle correctedHdg = Angle.FromDegrees(eulerAngles.X);
            if (DeviationCorrectionEnabled)
            {
                correctedHdg = _deviationCorrection.ToMagneticHeading(correctedHdg);
            }

            var correctedAngles = new Vector3((float)correctedHdg.Degrees, eulerAngles.Y, eulerAngles.Z);
            OnNewOrientation?.Invoke(correctedAngles);
        }

        protected override void UpdateSensors()
        {
            _pitch.Value = _lastEulerAngles.Z;
            _roll.Value = _lastEulerAngles.Y;
            Angle hdg = Angle.FromDegrees(_lastEulerAngles.X);
            _headingUncorrected.Value = hdg.Normalize(true).Degrees;
            
            hdg = _deviationCorrection.ToMagneticHeading(hdg);
            _heading.Value = hdg.Normalize(true).Degrees;
            _imuTemperature.Value = _imu.Temperature.DegreesCelsius;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_imu != null)
                {
                    _imu.OnNewData -= ImuOnNewData;
                    _imu.Dispose();
                    _serialPort.Dispose();
                    _imu = null;
                    _serialPort = null;
                }

                _correctionEnabled.Dispose();
                SensorValueSources.Clear();
            }

            base.Dispose(disposing);
        }
    }
}

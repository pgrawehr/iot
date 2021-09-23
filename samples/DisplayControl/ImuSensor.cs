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
using UnitsNet;

namespace DisplayControl
{
    /// <summary>
    /// This class uses polling only for GUI update
    /// </summary>
    internal class ImuSensor : PollingSensorBase
    {
        private readonly PersistentBool _correctionEnabled;
        private Ig500Sensor _imu;
        private SerialPort _serialPort;

        private Vector3 _lastEulerAngles;
        private MagneticDeviationCorrection _deviationCorrection;

        private SensorMeasurement _imuTemperature;

        public event Action<Vector3> OnNewOrientation;

        public ImuSensor(MeasurementManager manager, PersistenceFile file) : base(manager, TimeSpan.FromSeconds(1))
        {
            _lastEulerAngles = new Vector3();
            _imuTemperature = new SensorMeasurement("IMU Temperature", Temperature.Zero, SensorSource.Compass);
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

        /// <summary>
        /// Take value from the external heading sensor (it seems that the internal compass sometimes plays
        /// havoc, need to analyze)
        /// </summary>
        public bool PreferExternalHeading
        {
            get;
            set;
        }

        public Angle? ExternalHeading
        {
            get;
            set;
        }

        public override void Init(GpioController gpioController)
        {
            Manager.AddRange(new[]
            {
                SensorMeasurement.Heading, SensorMeasurement.HeadingRaw, SensorMeasurement.Roll,
                SensorMeasurement.Pitch, _imuTemperature, SensorMeasurement.Deviation, 
            });

            _deviationCorrection = new MagneticDeviationCorrection();
            _deviationCorrection.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Calibration_Cirrus_v3.xml"));

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

            if (!success || imu == null)
            {
                throw new IOException($"Error initializing IMU {errorMessage}");
            }

            _imu = imu;
            _imu.OnNewData += ImuOnNewData;

            base.Init(gpioController);
        }

        private void ImuOnNewData(Vector3 eulerAngles)
        {
            _lastEulerAngles = eulerAngles;
            GetHeadingAndDeviation(_lastEulerAngles, out Angle hdgUncorrected, out Angle hdg, out Angle deviation);

            var correctedAngles = new Vector3((float)hdg.Degrees, eulerAngles.Y, eulerAngles.Z);
            OnNewOrientation?.Invoke(correctedAngles);
        }

        protected override void UpdateSensors()
        {
            GetHeadingAndDeviation(_lastEulerAngles, out Angle hdgUncorrected, out Angle hdg, out Angle deviation);

            Manager.UpdateValues(new List<SensorMeasurement>()
            {
                SensorMeasurement.Pitch, SensorMeasurement.Roll, SensorMeasurement.Heading, SensorMeasurement.HeadingRaw,
                _imuTemperature
            },
            new List<IQuantity>()
            {
                Angle.FromDegrees(_lastEulerAngles.Z), Angle.FromDegrees(_lastEulerAngles.Y), 
                hdg.Normalize(true), hdgUncorrected.Normalize(true), _imu.Temperature
            });

            Manager.UpdateValue(SensorMeasurement.Deviation, deviation, DeviationCorrectionEnabled ? SensorMeasurementStatus.None : SensorMeasurementStatus.Warning);
        }

        private void GetHeadingAndDeviation(Vector3 angles, out Angle hdgUncorrected, out Angle hdg, out Angle deviation)
        {
            hdgUncorrected = Angle.FromDegrees(angles.X);
            hdg = hdgUncorrected;
            deviation = Angle.Zero;
            if (DeviationCorrectionEnabled)
            {
                hdg = _deviationCorrection.ToMagneticHeading(hdgUncorrected);
                deviation = (hdg - hdgUncorrected).Normalize(false);
            }

            if (PreferExternalHeading && ExternalHeading.HasValue)
            {
                hdg = ExternalHeading.Value;
                // Will show us the error of the internal sensor
                deviation = (hdg - hdgUncorrected).Normalize(false);
            }
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

                Manager.UpdateValues(new List<SensorMeasurement>()
                    {
                        SensorMeasurement.Pitch, SensorMeasurement.Roll, SensorMeasurement.Heading, SensorMeasurement.HeadingRaw,
                        _imuTemperature
                    },
                    new List<IQuantity>()
                    {
                        null, null, null, null, null
                    });
                _correctionEnabled.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

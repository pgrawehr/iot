using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Numerics;
using System.Text;
using Iot.Device.Common;
using Iot.Device.Imu;
using Iot.Device.Nmea0183;
using Iot.Device.Nmea0183.Sentences;
using UnitsNet;
using UnitsNet.Units;

namespace DisplayControl
{
    /// <summary>
    /// This class uses polling only for GUI update
    /// </summary>
    internal class ImuSensor : PollingSensorBase
    {
        private readonly PersistentBool _correctionEnabled;
        private readonly PersistentInt _headingMode;

        private Ig500Sensor _imu;
        private SerialPort _serialPort;

        private Vector3 _lastEulerAngles;
        private DateTimeOffset _lastTimeStamp;

        private MagneticDeviationCorrection _deviationCorrection;
        private List<HistoricValue> _deltaHistory;
        private List<HistoricValue> _deltaAccelXY;
        private List<HistoricValue> _deltaAccelZ;
        private object _lock = new object();

        private SensorMeasurement _imuTemperature;

        public event Action<Vector3> OnNewOrientation;

        public ImuSensor(MeasurementManager manager, PersistenceFile file) : base(manager, TimeSpan.FromSeconds(1))
        {
            _lastEulerAngles = new Vector3();
            _imuTemperature = new SensorMeasurement("IMU Temperature", Temperature.Zero, SensorSource.Compass);
            _correctionEnabled = new PersistentBool(file, "DeviationCorrectionEnabled", true);
            _headingMode = new PersistentInt(file, "HeadingMode", (int)HeadingMode.CompassCorrected);
            _deltaHistory = new List<HistoricValue>();
            _deltaAccelXY = new List<HistoricValue>();
            _deltaAccelZ = new List<HistoricValue>();
            _deltaHistory.Add(new HistoricValue(DateTimeOffset.UtcNow, Angle.Zero));
            RawHeading = Angle.Zero;
        }

        public bool DeviationCorrectionEnabled
        {
            get
            {
                return HeadingMode == HeadingMode.CompassCorrected;
            }
        }

        /// <summary>
        /// Operation mode
        /// </summary>
        public HeadingMode HeadingMode
        {
            get
            {
                return (HeadingMode)_headingMode.Value;
            }
            set
            {
                _headingMode.Value = (int)value;
            }
        }

        public HeadingAndDeclination ExternalHeading
        {
            get;
            set;
        }

        public Angle CurrentCourseOverGround
        {
            get;
            set;
        }

        public Angle RawHeading
        {
            get;
            private set;
        }

        public override void Init(GpioController gpioController)
        {
            Manager.AddRange(new[]
            {
                SensorMeasurement.Heading, SensorMeasurement.HeadingRaw, SensorMeasurement.Roll,
                SensorMeasurement.Pitch, _imuTemperature, SensorMeasurement.Deviation, SensorMeasurement.AccelerationXY, SensorMeasurement.AccelerationZ, 
            });

            _deviationCorrection = new MagneticDeviationCorrection();
            _deviationCorrection.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Calibration_Cirrus_2025.xml"));

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

        private void ImuOnNewData(Vector3 eulerAngles, Vector3 acceleration, DateTimeOffset timeStamp)
        {
            _lastEulerAngles = eulerAngles;
            _lastTimeStamp = timeStamp;
            var xyAccel = Math.Sqrt(acceleration.X * acceleration.X + acceleration.Y + acceleration.Y);

            Angle hdg;
            lock (_lock)
            {
                if (!double.IsNaN(xyAccel))
                {
                    _deltaAccelXY.Add(new HistoricValue(timeStamp,
                        Acceleration.FromMetersPerSecondSquared(xyAccel).ToUnit(AccelerationUnit.StandardGravity)));
                }

                if (!double.IsNaN(acceleration.Z))
                {
                    _deltaAccelZ.Add(new HistoricValue(timeStamp,
                        Acceleration.FromMetersPerSecondSquared(acceleration.Z)));
                }

                GetHeadingAndDeviation(_lastEulerAngles, out Angle hdgUncorrected, out hdg, out Angle deviation);
            }

            var correctedAngles = new Vector3((float)hdg.Degrees, eulerAngles.Y, eulerAngles.Z);
            OnNewOrientation?.Invoke(correctedAngles);
        }

        protected override void UpdateSensors()
        {
            lock (_lock)
            {
                GetHeadingAndDeviation(_lastEulerAngles, out Angle hdgUncorrected, out Angle hdg, out Angle deviation);

                _deltaAccelXY.RemoveOlderThan(TimeSpan.FromSeconds(10));
                _deltaAccelZ.RemoveOlderThan(TimeSpan.FromSeconds(10));

                Acceleration accelxy = Acceleration.Zero;
                if (_deltaAccelXY.Any())
                {
                    accelxy = ((Acceleration)_deltaAccelXY.MaxValue()).ToUnit(AccelerationUnit.StandardGravity);
                }

                Acceleration accelz = Acceleration.Zero;
                if (_deltaAccelZ.Any())
                {
                    accelz = ((Acceleration)_deltaAccelZ.AverageValue()).ToUnit(AccelerationUnit.StandardGravity);
                }

                Manager.UpdateValues(new List<SensorMeasurement>()
                    {
                        SensorMeasurement.Pitch,
                        SensorMeasurement.Roll,
                        SensorMeasurement.Heading,
                        SensorMeasurement.HeadingRaw,
                        _imuTemperature,
                        SensorMeasurement.AccelerationXY,
                        SensorMeasurement.AccelerationZ,
                    },
                    new List<IQuantity>()
                    {
                        Angle.FromDegrees(_lastEulerAngles.Z),
                        Angle.FromDegrees(_lastEulerAngles.Y),
                        hdg.Normalize(true),
                        hdgUncorrected.Normalize(true),
                        _imu.Temperature,
                        accelxy,
                        accelz
                    });

                RawHeading = hdgUncorrected.Normalize(true);

                Manager.UpdateValue(SensorMeasurement.Deviation, deviation, SensorMeasurementStatus.None);
            }
        }

        private void GetHeadingAndDeviation(Vector3 angles, out Angle hdgUncorrected, out Angle hdg, out Angle deviation)
        {
            hdgUncorrected = Angle.FromDegrees(angles.X);

            switch (HeadingMode)
            {
                case HeadingMode.Handheld:
                case HeadingMode.HandheldInverted:
                {
                    if (ExternalHeading != null && ExternalHeading.Declination.HasValue)
                    {
                        Angle hdgext = ExternalHeading.HeadingMagnetic.GetValueOrDefault(Angle.Zero);
                        hdgext =
                            (hdgext + Angle.FromDegrees(HeadingMode == HeadingMode.Handheld ? 0 : 180)).Normalize(true);
                        // Will show us the error of the internal sensor
                        deviation = (hdgext - hdgUncorrected).Normalize(false);
                        hdg = hdgext;
                    }
                    else
                    {
                        // Something is wrong (e.g handheld is not available) - switch back to normal mode
                        HeadingMode = HeadingMode.CompassCorrected;
                        hdg = hdgUncorrected;
                    }

                    break;
                } 
                case HeadingMode.CompassRaw:
                    hdg = hdgUncorrected;
                    deviation = Angle.Zero;
                    return;
                case HeadingMode.CompassCorrected:
                    // This should be the default case
                    hdg = hdgUncorrected;
                    break;
                case DisplayControl.HeadingMode.Course:
                    hdg = CurrentCourseOverGround;
                    break;
                default:
                    throw new InvalidOperationException("Invalid heading mode");
            }

            deviation = Angle.Zero;
            if (DeviationCorrectionEnabled)
            {
                hdg = _deviationCorrection.ToMagneticHeading(hdgUncorrected);
                deviation = (hdg - hdgUncorrected).Normalize(false);
            }
        }

        /// <summary>
        /// Calculate heading from multiple inputs (currently only two heading sensors)
        /// </summary>
        /// <param name="inputs">List of inputs</param>
        /// <param name="output">Determined heading</param>
        /// <returns>True on success, false otherwise</returns>
        private bool MangleHeadingAngles(List<(HeadingAndDeclination Heading, double Quality)> inputs, out Angle output)
        {
            lock (_lock)
            {
                output = Angle.Zero;
                if (inputs.Count == 0)
                {
                    return false;
                }

                if (inputs.Count == 1 || inputs.Count > 2) // unsupported cases for now
                {
                    var e = inputs[0].Heading;
                    output = e.HeadingMagnetic.GetValueOrDefault(Angle.Zero);
                    return e.Valid && e.HeadingMagnetic.HasValue;
                }

                // exactly two elements
                var ext = inputs[1].Heading;
                var imu = inputs[0].Heading;
                if (ext.Age <= TimeSpan.FromSeconds(2) && ext.HeadingMagnetic.HasValue && imu.HeadingMagnetic.HasValue)
                {
                    // This will only be the case when the ext data is very new
                    var delta = (ext.HeadingMagnetic.Value - imu.HeadingMagnetic.Value).Normalize(false);
                    _deltaHistory.Add(new HistoricValue(ext.DateTime.DateTime, delta));
                    _deltaHistory.RemoveOlderThan(TimeSpan.FromSeconds(60), 2);
                }

                var historicDelta = Angle.FromDegrees(_deltaHistory.AverageValue().Value);
                // Now estimated an average delta between the two. Now use the imu (faster) to compute the real value using that delta
                var result = (imu.HeadingMagnetic.GetValueOrDefault(Angle.Zero) + historicDelta).Normalize(true);
                output = result;
                return true;
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

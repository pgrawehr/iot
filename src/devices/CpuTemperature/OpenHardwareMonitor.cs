using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.CpuTemperature
{
    public enum SensorType
    {
        Unknown,
        Voltage,
        Clock,
        Temperature,
        Load,
        Fan,
        Flow,
        Control,
        Level,
        Power,
    }

    public sealed class OpenHardwareMonitor
    {
        private bool _isAvalable;

        private delegate IQuantity UnitCreator(float value);

        private static Dictionary<SensorType, (Type Type, UnitCreator Creator)> _typeMap;
        private Hardware _cpu;
        private Hardware _gpu;

        static OpenHardwareMonitor()
        {
            _typeMap = new Dictionary<SensorType, (Type Type, UnitCreator Creator)>();
            _typeMap.Add(SensorType.Temperature, (typeof(Temperature), (x) => Temperature.FromDegreesCelsius(x)));
            _typeMap.Add(SensorType.Voltage, (typeof(ElectricPotential), x => ElectricPotential.FromVolts(x)));
            _typeMap.Add(SensorType.Load, (typeof(Ratio), x => Ratio.FromPercent(x)));
            _typeMap.Add(SensorType.Fan, (typeof(RotationalSpeed), x => RotationalSpeed.FromRevolutionsPerMinute(x)));
            _typeMap.Add(SensorType.Flow, (typeof(VolumeFlow), x => VolumeFlow.FromLitersPerHour(x)));
            _typeMap.Add(SensorType.Control, (typeof(Ratio), x => Ratio.FromPercent(x)));
            _typeMap.Add(SensorType.Level, (typeof(Ratio), x => Ratio.FromPercent(x)));
            _typeMap.Add(SensorType.Power, (typeof(Power), x => Power.FromWatts(x)));
            _typeMap.Add(SensorType.Clock, (typeof(Frequency), x => Frequency.FromMegahertz(x)));
        }

        public OpenHardwareMonitor()
        {
            _isAvalable = false;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("This class is only supported on Windows operating systems");
            }

            try
            {
                // This works only if OpenHardwareMonitor (https://openhardwaremonitor.org/) is currently running, but
                // has the advantage of being supported on a wide number of platforms and works without elevation.
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\OpenHardwareMonitor", "SELECT * FROM Sensor");
                if (searcher.Get().Count > 0)
                {
                    _isAvalable = true;
                    foreach (var hardware in GetHardwareComponents())
                    {
                        if (hardware.Type.Equals("CPU", StringComparison.OrdinalIgnoreCase))
                        {
                            _cpu = hardware;
                        }
                        else if (hardware.Type.StartsWith("GPU", StringComparison.OrdinalIgnoreCase))
                        {
                            _gpu = hardware;
                        }
                    }
                }
            }
            catch (Exception x) when (x is IOException || x is UnauthorizedAccessException || x is ManagementException)
            {
                // Nothing to do - WMI not available for this element or missing permissions.
                // WMI enumeration may require elevated rights.
            }
        }

        public bool IsAvailable => _isAvalable;

        /// <summary>
        /// Query the list of all available sensors.
        /// </summary>
        /// <returns>A list of <see cref="Sensor"/> instances. May be empty.</returns>
        /// <exception cref="ManagementException">The WMI objects required are not available. Is OpenHardwareMonitor running?</exception>
        public IList<Sensor> GetSensorList()
        {
            IList<Sensor> ret = new List<Sensor>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\OpenHardwareMonitor", "SELECT * FROM Sensor");
            if (searcher.Get().Count > 0)
            {
                foreach (ManagementObject sensor in searcher.Get())
                {
                    string name = Convert.ToString(sensor["Name"]);
                    string identifier = Convert.ToString(sensor["Identifier"]);
                    string parent = Convert.ToString(sensor["Parent"]);
                    string type = Convert.ToString(sensor["SensorType"]);
                    SensorType typeEnum;
                    if (!Enum.TryParse(type, true, out typeEnum))
                    {
                        typeEnum = SensorType.Unknown;
                    }

                    ret.Add(new Sensor(sensor, name, identifier, parent, typeEnum));
                }
            }

            return ret;
        }

        public IList<Hardware> GetHardwareComponents()
        {
            IList<Hardware> ret = new List<Hardware>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\OpenHardwareMonitor", "SELECT * FROM Hardware");
            if (searcher.Get().Count > 0)
            {
                foreach (ManagementObject sensor in searcher.Get())
                {
                    string name = Convert.ToString(sensor["Name"]);
                    string identifier = Convert.ToString(sensor["Identifier"]);
                    string parent = Convert.ToString(sensor["Parent"]);
                    string type = Convert.ToString(sensor["HardwareType"]);
                    ret.Add(new Hardware(name, identifier, parent, type));
                }
            }

            return ret;
        }

        public IEnumerable<Sensor> GetSensorList(Hardware forHardware)
        {
            return GetSensorList().Where(x => x.Identifier.StartsWith(forHardware.Identifier)).OrderBy(y => y.Identifier);
        }

        // Some well-known properties have their own method

        /// <summary>
        /// Gets the average CPU temperature (averaged over all CPU sensors / cores)
        /// </summary>
        public bool TryGetAverageCpuTemperature(out Temperature temperature)
        {
            if (TryGetAverage(_cpu, out temperature))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the average CPU temperature (averaged over all CPU sensors / cores)
        /// </summary>
        /// <param name="temperature">The average GPU temperature</param>
        public bool TryGetAverageGpuTemperature(out Temperature temperature)
        {
            if (TryGetAverage(_gpu, out temperature))
            {
                return true;
            }

            return false;
        }

        public Ratio GetCpuLoad()
        {
            foreach (var s in GetSensorList(_cpu).OrderBy(x => x.Identifier))
            {
                if (s.SensorType == SensorType.Load && s.TryGetValue(out Ratio load))
                {
                    return load;
                }
            }

            return default(Ratio);
        }

        public bool TryGetAverage<T>(Hardware hardware, out T average)
            where T : IQuantity
        {
            double value = 0;
            int count = 0;
            Enum unitThatWasUsed = null;
            foreach (var s in GetSensorList(hardware))
            {
                if (s.TryGetValue(out T singleValue))
                {
                    if (unitThatWasUsed == null)
                    {
                        unitThatWasUsed = singleValue.Unit;
                    }
                    else if (!unitThatWasUsed.Equals(singleValue.Unit))
                    {
                        throw new NotSupportedException($"The different sensors for {hardware.Name} deliver values in different units");
                    }

                    value += singleValue.Value;
                    count++;
                }
            }

            if (count == 0)
            {
                average = default(T);
                return false;
            }

            value = value / count;

            average = (T)Quantity.From(value, unitThatWasUsed);
            return true;
        }

        public sealed class Sensor : IDisposable
        {
            private readonly ManagementObject _instance;

            public Sensor(ManagementObject instance, string name, string identifier, string parent, SensorType typeEnum)
            {
                _instance = instance;
                Name = name;
                Identifier = identifier;
                Parent = parent;
                SensorType = typeEnum;
            }

            public string Name { get; }
            public string Identifier { get; }
            public string Parent { get; }
            public SensorType SensorType { get; }

            public bool TryGetValue(out IQuantity value)
            {
                if (!_typeMap.TryGetValue(SensorType, out var elem))
                {
                    value = null;
                    return false;
                }

                float newValue = Convert.ToSingle(_instance["Value"]);
                IQuantity newValueAsUnitInstance = elem.Creator(newValue);

                value = newValueAsUnitInstance;
                return true;
            }

            public bool TryGetValue<T>(out T value)
                where T : IQuantity
            {
                if (!_typeMap.TryGetValue(SensorType, out var elem))
                {
                    value = default(T);
                    return false;
                }

                if (typeof(T) != elem.Type)
                {
                    value = default(T);
                    return false;
                }

                float newValue = Convert.ToSingle(_instance["Value"]);
                object newValueAsUnitInstance = elem.Creator(newValue);

                value = (T)newValueAsUnitInstance;
                return true;
            }

            public void Dispose()
            {
                _instance.Dispose();
            }
        }

        public sealed class Hardware
        {
            public Hardware(string name, string identifier, string parent, string type)
            {
                Name = name;
                Identifier = identifier;
                Parent = parent;
                Type = type;
            }

            public string Name { get; }
            public string Identifier { get; }
            public string Parent { get; }
            public string Type { get; }
        }
    }
}

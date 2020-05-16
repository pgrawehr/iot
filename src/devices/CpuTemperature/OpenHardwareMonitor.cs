using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Iot.Units;

#pragma warning disable CS1591
namespace CpuTemperature
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

        /// <summary>
        /// Return value needs to be object, because we cannot create a list of untyped methods
        /// </summary>
        private delegate object UnitCreator(float value);

        private static Dictionary<SensorType, (Type Type, UnitCreator Creator)> _typeMap;

        static OpenHardwareMonitor()
        {
            // TODO: Use proper types
            _typeMap = new Dictionary<SensorType, (Type Type, UnitCreator Creator)>();
            _typeMap.Add(SensorType.Temperature, (typeof(Temperature), (x) => Temperature.FromCelsius(x)));
            _typeMap.Add(SensorType.Voltage, (typeof(float), x => x));
            _typeMap.Add(SensorType.Load, (typeof(float), x => x));
            _typeMap.Add(SensorType.Fan, (typeof(float), x => x));
            _typeMap.Add(SensorType.Flow, (typeof(float), x => x));
            _typeMap.Add(SensorType.Control, (typeof(float), x => x));
            _typeMap.Add(SensorType.Level, (typeof(float), x => x));
            _typeMap.Add(SensorType.Power, (typeof(float), x => x));
            _typeMap.Add(SensorType.Clock, (typeof(float), x => x));
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
            return GetSensorList().Where(x => x.Identifier.StartsWith(forHardware.Identifier));
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
                TypeEnum = typeEnum;
            }

            public string Name { get; }
            public string Identifier { get; }
            public string Parent { get; }
            public SensorType TypeEnum { get; }

            public bool TryGetValue<T>(out T value)
            {
                if (!_typeMap.TryGetValue(TypeEnum, out var elem))
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

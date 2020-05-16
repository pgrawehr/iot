// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using UnitsNet;

namespace Iot.Device.CpuTemperature
{
    /// <summary>
    /// CPU temperature
    /// </summary>
    public class CpuTemperature
    {
        private bool _isAvalable = false;
        private bool _checkedIfAvailable = false;
        private bool _windows = false;

        /// <summary>
        /// Gets CPU temperature
        /// </summary>
        public Temperature Temperature
        {
            get
            {
                if (!_windows)
                {
                    return Temperature.FromCelsius(ReadTemperatureUnix());
                }
                else
                {
                    List<(string, Temperature)> tempList = ReadTemperatures();
                    return tempList.FirstOrDefault().Item2;
                }
            }
        }

        private List<ManagementObjectSearcher> _managementObjectSearchers = new List<ManagementObjectSearcher>();

        /// <summary>
        /// Is CPU temperature available
        /// </summary>
        public bool IsAvailable => CheckAvailable();

        private bool CheckAvailable()
        {
            if (!_checkedIfAvailable)
            {
                _checkedIfAvailable = true;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/sys/class/thermal/thermal_zone0/temp"))
                {
                    _isAvalable = true;
                    _windows = false;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");
                        if (searcher.Get().Count > 0)
                        {
                            _managementObjectSearchers.Add(searcher);
                            _isAvalable = true;
                            _windows = true;
                        }
                    }
                    catch (Exception x) when (x is IOException || x is UnauthorizedAccessException || x is ManagementException)
                    {
                        // Nothing to do - WMI not available for this element or missing permissions.
                        // WMI enumeration may require elevated rights.
                    }

                    try
                    {
                        ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM Win32_TemperatureProbe");
                        if (searcher.Get().Count > 0)
                        {
                            _managementObjectSearchers.Add(searcher);
                            _isAvalable = true;
                            _windows = true;
                        }
                    }
                    catch (Exception x) when (x is IOException || x is UnauthorizedAccessException || x is ManagementException)
                    {
                        // Nothing to do - WMI not available for this element or missing permissions.
                        // WMI enumeration may require elevated rights.
                    }

                }
            }

            return _isAvalable;
        }

        /// <summary>
        /// Returns all temperature sensor values found
        /// </summary>
        /// <returns>A list of name/value pairs for temperature sensors</returns>
        public List<(string, Temperature)> ReadTemperatures()
        {
            if (!_windows)
            {
                var ret = new List<(string, Temperature)>();
                ret.Add(("CPU", Temperature.FromCelsius(ReadTemperatureUnix())));
                return ret;
            }

            // Windows code below
            List<(string, Temperature)> result = new List<(string, Temperature)>();

            foreach (var searcher in _managementObjectSearchers)
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    Double temp = Convert.ToDouble(string.Format(CultureInfo.InvariantCulture, "{0}", obj["CurrentTemperature"]), CultureInfo.InvariantCulture);
                    temp = (temp - 2732) / 10.0;
                    result.Add((obj["InstanceName"].ToString(), Temperature.FromCelsius(temp)));
                }
            }

            return result;
        }

        private double ReadTemperatureUnix()
        {
            double temperature = double.NaN;

            if (CheckAvailable())
            {
                using (FileStream fileStream = new FileStream("/sys/class/thermal/thermal_zone0/temp", FileMode.Open, FileAccess.Read))
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    string data = reader.ReadLine();
                    if (!string.IsNullOrEmpty(data))
                    {
                        int temp;
                        if (int.TryParse(data, out temp))
                        {
                            temperature = temp / 1000F;
                        }
                    }
                }
            }

            return temperature;
        }
    }
}

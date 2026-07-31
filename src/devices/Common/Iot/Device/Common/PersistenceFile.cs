// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Iot.Device.Common
{
    public sealed class PersistenceFile
    {
        private string _fileName;
        private object _fileLock;
        public PersistenceFile(string fileName)
        {
            _fileName = fileName;
            _fileLock = new object();
        }

        internal void SaveValue<T>(string name, Serializer<T> serializer, T value)
        {
            lock (_fileLock)
            {
                using (StreamWriter w = new StreamWriter(_fileName, true))
                {
                    string dataLine = String.Format(CultureInfo.InvariantCulture, "{0:s}|{1}|{2}|$", DateTime.Now, name, serializer(value));
                    w.WriteLine(dataLine);
                    w.Flush();
                }
            }
        }

        internal T GetLastValue<T>(string name, Deserializer<T> deserializer, T initialValue)
        {
            var allValues = GetAllValues(name, deserializer);
            if (allValues.Count == 0)
            {
                return initialValue;
            }

            return allValues.Last().Element;
        }

        public List<(DateTime TimeStamp, T Element)> GetAllValues<T>(string name, Deserializer<T> deserializer)
        {
            List<(DateTime, T)> ret = new List<(DateTime, T)>();
            lock (_fileLock)
            {
                if (!File.Exists(_fileName))
                {
                    return ret;
                }

                using (StreamReader r = new StreamReader(_fileName, true))
                {
                    string? line = r.ReadLine();
                    while (line != null)
                    {
                        string[] splits = line.Split(new char[] { '|' }, StringSplitOptions.None);
                        if (splits.Length == 4 && line.IndexOf('$') > 0)
                        {
                            string time = splits[0];
                            DateTime? timeStamp = null;
                            if (DateTime.TryParse(time, CultureInfo.InvariantCulture, out var t))
                            {
                                timeStamp = t;
                            }

                            string valueName = splits[1];
                            if (valueName == name && timeStamp.HasValue)
                            {
                                string toDeserialze = splits[2];
                                if (deserializer(toDeserialze, out T v))
                                {
                                    ret.Add((timeStamp.Value, v));
                                }
                            }
                        }

                        line = r.ReadLine();
                    }
                }

                return ret;
            }
        }
    }
}

#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;

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
            lock (_fileLock)
            {
                if (!File.Exists(_fileName))
                {
                    return initialValue;
                }

                T lastValue = initialValue;
                using (StreamReader r = new StreamReader(_fileName, true))
                {
                    string? line = r.ReadLine();
                    while (line != null)
                    {
                        string[] splits = line.Split(new char[] { '|' }, StringSplitOptions.None);
                        if (splits.Length == 4 && line.IndexOf('$') > 0)
                        {
                            string valueName = splits[1];
                            if (valueName == name)
                            {
                                string toDeserialze = splits[2];
                                if (deserializer(toDeserialze, out T v))
                                {
                                    lastValue = v;
                                }
                            }
                        }

                        line = r.ReadLine();
                    }
                }

                return lastValue;
            }
        }
    }
}

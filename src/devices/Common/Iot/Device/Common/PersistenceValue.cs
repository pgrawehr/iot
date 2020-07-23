using System;
using System.IO;

#pragma warning disable CS1591
namespace Iot.Device.Persistence
{
    public delegate string Serializer<T>(T value);
    public delegate bool Deserializer<T>(string data, out T value);

    public class PersistentValue<T>
    {
        private readonly PersistenceFile _file;
        private readonly Serializer<T> _serializer;
        private readonly Deserializer<T> _deserializer;
        private int _lastSave;
        private T _value;
        private TimeSpan _saveInterval;

        public PersistentValue(PersistenceFile file, string name, T initialValue, TimeSpan saveInterval, Serializer<T> serializer, Deserializer<T> deserializer)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
            Name = name;
            SaveInterval = saveInterval;

            _serializer = serializer;
            _deserializer = deserializer;
            _lastSave = Environment.TickCount;
            _value = file.GetLastValue(name, _deserializer, initialValue);
        }

        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                int now = Environment.TickCount;
                if (_lastSave + SaveInterval.TotalMilliseconds < now || now < _lastSave)
                {
                    _file.SaveValue(Name, _serializer, _value);
                    _lastSave = now;
                }
            }
        }

        public string Name
        {
            get;
        }

        public TimeSpan SaveInterval
        {
            get
            {
                return _saveInterval;
            }

            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("Save interval must be positive");
                }

                _saveInterval = value;
            }
        }

        /// <summary>
        /// Explicitly persists the value
        /// </summary>
        public void Save()
        {
            _file.SaveValue(Name, _serializer, _value);
        }
    }
}

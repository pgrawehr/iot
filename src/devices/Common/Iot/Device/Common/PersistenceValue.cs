// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    public delegate string Serializer<T>(T value);
    public delegate bool Deserializer<T>(string data, out T value);

    /// <summary>
    /// Keeps a value persistent across application sessions. Useful i.e. to store a total run time of an application
    /// or the total number of items produced. Will also be (approximately) exact if power is lost unexpectedly.
    /// </summary>
    /// <typeparam name="T">Type of value to store</typeparam>
    public class PersistentValue<T> : IDisposable
    {
        private readonly Serializer<T> _serializer;
        private readonly Deserializer<T> _deserializer;
        private PersistenceFile? _file;
        private int _lastSave;
        private T _value;
        private TimeSpan _saveInterval;

        /// <summary>
        /// Creates an instance of a persisting value. Preferably use subclasses instead.
        /// </summary>
        /// <param name="file">File that store the data (will only ever grow for now). Pass null if storing is not required (i.e. for tests)</param>
        /// <param name="name">Name of the parameter. Should be unique for the application.</param>
        /// <param name="initialValue">Initial value, if nothing has ever been stored with this name.</param>
        /// <param name="saveInterval">Interval for persisting the value.</param>
        /// <param name="serializer">Serializer callback (usually something like <see cref="Object.ToString()"/> with a matching format)</param>
        /// <param name="deserializer">Deserializer callback (usually a type.TryParse() call)</param>
        public PersistentValue(PersistenceFile? file, string name, T initialValue, TimeSpan saveInterval, Serializer<T> serializer, Deserializer<T> deserializer)
        {
            _file = file;
            Name = name;
            SaveInterval = saveInterval;

            _serializer = serializer;
            _deserializer = deserializer;
            _lastSave = Environment.TickCount;
            if (file != null)
            {
                _value = file.GetLastValue(name, _deserializer, initialValue);
            }
            else
            {
                _value = initialValue;
            }
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
                if (_lastSave + SaveInterval.TotalMilliseconds < now || now < _lastSave || SaveInterval.TotalMilliseconds <= 0)
                {
                    _file?.SaveValue(Name, _serializer, _value);
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
                    throw new ArgumentOutOfRangeException(nameof(value), "Save interval must be positive");
                }

                _saveInterval = value;
            }
        }

        /// <summary>
        /// Explicitly persists the value
        /// </summary>
        public void Save()
        {
            _file?.SaveValue(Name, _serializer, _value);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Make sure we save the last value (there might have been value updates that were not persisted yet)
                Save();
            }

            _file = null!;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

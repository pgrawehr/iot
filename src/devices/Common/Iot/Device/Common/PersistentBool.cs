using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Iot.Device.Common
{
    /// <summary>
    /// Persists a boolean value (it will always keep its last set value)
    /// </summary>
    public class PersistentBool : PersistentValue<bool>
    {
        /// <summary>
        /// Creates a new instance of this type.
        /// </summary>
        /// <param name="file">Name of the persistence file</param>
        /// <param name="name">Value name</param>
        /// <param name="initialValue">Initial value</param>
        public PersistentBool(PersistenceFile file, string name, bool initialValue)
            : base(file, name, initialValue, TimeSpan.Zero, Serializer, Deserializer)
        {
        }

        private static bool Deserializer(string data, out bool value)
        {
            return Boolean.TryParse(data, out value);
        }

        private static string Serializer(bool value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}

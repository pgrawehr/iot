using System;
using System.Globalization;
using System.IO;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    public class PersistentDouble : PersistentValue<double>
    {
        public PersistentDouble(PersistenceFile file, string name, double initialValue, TimeSpan saveInterval)
            : base(file, name, initialValue, saveInterval, Serializer, Deserializer)
        {
        }

        private static string Serializer(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static bool Deserializer(string data, out double value)
        {
            return double.TryParse(data, out value);
        }
    }
}

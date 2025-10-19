using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Adc
{
    /// <summary>
    /// Base class for current/voltage sensors
    /// </summary>
    public class Ina2xx : IDisposable
    {
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }
    }
}

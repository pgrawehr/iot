using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    public enum StandardFilterAction
    {
        Unknown,
        DiscardMessage,
        ForwardToAllOthers,
        ForwardToAll,
        SendBack,

        /// <summary>
        /// Forward to local instance only
        /// </summary>
        ForwardToLocal,

        /// <summary>
        /// Forward to primary interface (that is the first registered one)
        /// </summary>
        ForwardToPrimary,
        ForwardToSecondary,
        ForwardToTernary,
    }
}

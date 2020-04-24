using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591
namespace Nmea0183
{
    public enum StandardFilterAction
    {
        Unknown,
        DiscardMessage,
        ForwardToAllOthers,
        ForwardToAll,
        SendBack
    }
}

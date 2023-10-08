﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SA1403 // File may only contain a single namespace

// These instructions make the classes public for building the individual projects.
// The default visibility is "internal" so they stay hidden if this section is removed for the final library build.
#if !BUILDING_IOT_DEVICE_BINDINGS

/// <content>
/// Visibility declaration only
/// </content>
public partial class Interop
{
}

namespace Iot.Device.Common
{
    /// <content>
    /// Visibility declaration only
    /// </content>
    public partial class NumberHelper
    {
    }
}

namespace System.Device
{
    /// <content>
    /// Visibility declaration only
    /// </content>
    public partial class DelayHelper
    {
    }
}

namespace System.Device.Gpio
{
    /// <content>
    /// Visibility declaration only
    /// </content>
    public partial struct PinVector32
    {
    }

    /// <content>
    /// Visibility declaration only
    /// </content>
    public partial struct PinVector64
    {
    }
}

#else

/// <content>
/// Visibility declaration only
/// </content>
internal partial class Interop
{
}

namespace Iot.Device.Common
{
    /// <content>
    /// Visibility declaration only
    /// </content>
    internal partial class NumberHelper
    {
    }
}

namespace System.Device
{
    /// <content>
    /// Visibility declaration only
    /// </content>
    internal partial class DelayHelper
    {
    }
}

namespace System.Device.Gpio
{
    /// <content>
    /// Visibility declaration only
    /// </content>
    internal partial struct PinVector32
    {
    }

    /// <content>
    /// Visibility declaration only
    /// </content>
    internal partial struct PinVector64
    {
    }
}


#endif

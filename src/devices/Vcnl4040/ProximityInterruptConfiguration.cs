﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Iot.Device.Vcnl4040.Definitions;

/// <summary>
/// ...
/// </summary>
public record ProximityInterruptConfiguration(
    int LowerThreshold,
    int UpperThreshold,
    PsInterruptPersistence Persistence,
    bool SmartPersistenceEnabled,
    ProximityInterruptMode Mode);
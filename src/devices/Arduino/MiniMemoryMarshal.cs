﻿using System;

namespace Iot.Device.Arduino
{
    [ArduinoReplacement(typeof(System.Runtime.InteropServices.MemoryMarshal), false, IncludingPrivates = true)]
    internal static class MiniMemoryMarshal
    {
        [ArduinoImplementation(NativeMethod.MemoryMarshalGetArrayDataReference, CompareByParameterNames = true, IgnoreGenericTypes = true)]
        public static ref T GetArrayDataReference<T>(T[] array)
        {
            throw new NotImplementedException();
        }
    }
}
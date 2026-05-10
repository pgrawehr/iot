// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ArduinoCsCompiler.Runtime
{
    internal partial class MiniInterop
    {
        [ArduinoReplacement("Interop+Kernel32", "System.Private.CoreLib.dll", true, IncludingPrivates = true, TargetFramework = TargetFramework.Nano)]
        internal static partial class Kernel32_Nano
        {
            internal static unsafe int LCMapStringEx(string lpLocaleName, uint dwMapFlags, char* lpSrcStr, int cchsrc, void* lpDestStr,
                int cchDest, void* lpVersionInformation, void* lpReserved, IntPtr sortHandle)
            {
                return 0; // Can apparently be null in InvariantCulture mode.
            }

            public static bool SetEvent(SafeWaitHandle handle)
            {
                throw new NotImplementedException();
            }

            [ArduinoImplementation(CompareByParameterNames = true)]
            internal static unsafe void InitializeCriticalSection(
                CRITICAL_SECTION* lpCriticalSection)
            {
                throw new NotImplementedException();
            }

            [ArduinoImplementation(CompareByParameterNames = true)]
            internal static unsafe void EnterCriticalSection(
                CRITICAL_SECTION* lpCriticalSection)
            {
                throw new NotImplementedException();
            }

            [ArduinoImplementation(CompareByParameterNames = true)]
            internal static unsafe void LeaveCriticalSection(
                CRITICAL_SECTION* lpCriticalSection)
            {
                throw new NotImplementedException();
            }

            [ArduinoImplementation(CompareByParameterNames = true)]
            internal static unsafe void DeleteCriticalSection(
                CRITICAL_SECTION* lpCriticalSection)
            {
                throw new NotImplementedException();
            }

            [ArduinoImplementation(CompareByParameterNames = true)]
            internal static unsafe bool SleepConditionVariableCS(
                CONDITION_VARIABLE* ConditionVariable,
                CRITICAL_SECTION* CriticalSection,
                int dwMilliseconds)
            {
                throw new NotImplementedException();
            }

            [ArduinoImplementation(CompareByParameterNames = true)]
            internal static unsafe void InitializeConditionVariable(
                CONDITION_VARIABLE* ConditionVariable)
            {
                throw new NotImplementedException();
            }

            [ArduinoImplementation(CompareByParameterNames = true)]
            internal static unsafe void WakeConditionVariable(
                CONDITION_VARIABLE* ConditionVariable)
            {
                throw new NotImplementedException();
            }

            [ArduinoImplementation]
            public static string GetMessage(int errorCode)
            {
                // We don't have the resources for the full messages available
                return string.Format("OS error (0x{0:x})", errorCode);
            }

            [ArduinoImplementation]
            public static string GetMessage(int errorCode, IntPtr moduleHandle)
            {
                return string.Format("OS error (0x{0:x})", errorCode);
            }

            [ArduinoImplementation]
            internal static unsafe bool QueryPerformanceFrequency(long* lpFrequency)
            {
                *lpFrequency = 10000 * 1000; // One tick is 100 ns (one 10th of a microsecond)
                return true;
            }

            [ArduinoImplementation]
            internal static unsafe bool QueryPerformanceCounter(long* lpCounter)
            {
                *lpCounter = Environment.TickCount64 * 10000;
                return true;
            }
        }
    }
}

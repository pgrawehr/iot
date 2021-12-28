// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Interop.Windows
{
    /// <summary>
    /// Native Methods for Windows
    /// </summary>
    internal partial class NativeMethods
    {
        /// <summary>
        /// Sets the system time (current user must be admin or have the special priviledge to set the time)
        /// </summary>
        /// <param name="lpSystemTime">The new system time</param>
        /// <returns>True on success, false otherwise</returns>
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetSystemTime([In] ref SystemTime lpSystemTime);

        /// <summary>
        /// Gets the system time from the kernel
        /// </summary>
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSystemTime([Out] out SystemTime lpSystemTime);

        /// <summary>
        /// This structure is used by <see cref="NativeMethods.SetSystemTime"/> and <see cref="NativeMethods.GetSystemTime"/>
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct SystemTime
        {
            /// <summary>
            /// Year
            /// </summary>
            public ushort Year;

            /// <summary>
            /// Month
            /// </summary>
            public ushort Month;

            /// <summary>
            /// Day of week
            /// </summary>
            public ushort DayOfWeek;

            /// <summary>
            /// Day of the month
            /// </summary>
            public ushort Day;

            /// <summary>
            /// Hour
            /// </summary>
            public ushort Hour;

            /// <summary>
            /// Minute
            /// </summary>
            public ushort Minute;

            /// <summary>
            /// Second
            /// </summary>
            public ushort Second;

            /// <summary>
            /// Milliseconds
            /// </summary>
            public ushort Milliseconds;
        }
    }
}

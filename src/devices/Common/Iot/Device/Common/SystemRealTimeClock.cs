// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interop.Windows;

namespace Iot.Device.Common
{
    /// <summary>
    /// Contains methods to access and update the system real time clock ("Bios clock")
    /// </summary>
    public static class SystemRealTimeClock
    {
        private static readonly DateTime UnixEpochStart = new DateTime(1970, 1, 1);

        /// <summary>
        /// Set the system time to the given date/time.
        /// The time must be given in utc.
        /// The method requires elevated permissions. On Windows, the calling user must either be administrator or the right
        /// "Change the system clock" must have been granted to the "Users" group (in Security policy management).
        /// </summary>
        /// <param name="dt">Date/time to set the system clock to. This must be in UTC</param>
        /// <returns>True on success, false on failure. This fails if the current user has insufficient rights to set the system clock. </returns>
        public static bool SetSystemTimeUtc(DateTime dt)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return SetSystemTimeUtcWindows(dt);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return SetDateTimeUtcUnix(dt);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the current system time directly using OS calls.
        /// Normally, this should return the same as <see cref="DateTime.UtcNow"/>
        /// </summary>
        /// <param name="dt">[Out] The current system time in UTC if the call succeeds</param>
        /// <returns>True on success, false otherwise</returns>
        public static bool GetSystemTimeUtc(out DateTime dt)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return GetSystemTimeUtcWindows(out dt);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return GetDateTimeUtcUnix(out dt);
            }
            else
            {
                dt = default;
                return false;
            }
        }

        private static bool SetSystemTimeUtcWindows(DateTime dt)
        {
            bool isSuccess = false;
            NativeMethods.SystemTime st = DateTimeToSystemTime(dt);
            try
            {
                isSuccess = NativeMethods.SetSystemTime(ref st);
            }
            catch (System.UnauthorizedAccessException)
            {
                isSuccess = false;
            }
            catch (System.Security.SecurityException)
            {
                isSuccess = false;
            }

            return isSuccess;
        }

        private static bool GetSystemTimeUtcWindows(out DateTime dt)
        {
            bool isSuccess = false;
            NativeMethods.SystemTime st;
            dt = default;
            try
            {
                isSuccess = NativeMethods.GetSystemTime(out st);
                dt = SystemTimeToDateTime(ref st);
            }
            catch (System.UnauthorizedAccessException)
            {
                isSuccess = false;
            }
            catch (System.Security.SecurityException)
            {
                isSuccess = false;
            }

            return isSuccess;
        }

        private static NativeMethods.SystemTime DateTimeToSystemTime(DateTime dt)
        {
            NativeMethods.SystemTime st;

            st.Year = (ushort)dt.Year;
            st.Day = (ushort)dt.Day;
            st.Month = (ushort)dt.Month;
            st.Hour = (ushort)dt.Hour;
            st.Minute = (ushort)dt.Minute;
            st.Second = (ushort)dt.Second;
            st.Milliseconds = (ushort)dt.Millisecond;
            st.DayOfWeek = (ushort)dt.DayOfWeek;

            return st;
        }

        private static DateTime SystemTimeToDateTime(ref NativeMethods.SystemTime st)
        {
            return new DateTime(st.Year, st.Month, st.Day, st.Hour, st.Minute, st.Second, st.Milliseconds);
        }

        private static bool GetDateTimeUtcUnix(out DateTime dt)
        {
            try
            {
                string date = RunDateCommandUnix("-u +%s.%N", false); // Floating point seconds since epoch
                if (Double.TryParse(date, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    dt = UnixEpochStart + TimeSpan.FromSeconds(result);
                    return true;
                }

                dt = default;
                return false;
            }
            catch (IOException)
            {
                dt = default;
                return false;
            }
        }

        private static bool SetDateTimeUtcUnix(DateTime dt)
        {
            string formattedTime = dt.ToString("yyyy-MM-dd HH:mm:ss.fffff", CultureInfo.InvariantCulture);
            try
            {
                RunDateCommandUnix($"-u -s '{formattedTime}'", true);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static string RunDateCommandUnix(string commandLine, bool asRoot)
        {
            var si = new ProcessStartInfo()
            {
                FileName = asRoot ? "sudo" : "date",
                Arguments = asRoot ? "-n date " + commandLine : commandLine,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var process = new Process();
            string outputData;
            process.StartInfo = si;
            {
                process.Start();
                outputData = process.StandardOutput.ReadToEnd();
                var errorData = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new IOException($"Error running date command. Error {process.ExitCode}: {errorData}");
                }
            }

            return outputData;
        }
    }
}

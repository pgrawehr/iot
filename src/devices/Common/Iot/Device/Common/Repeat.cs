// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    /// <summary>
    /// Helps repeating an operation that can possibly fail.
    /// An operation has failed if either
    /// - it returns false
    /// - it throws a recoverable system error, namely IOException or TimeoutException
    /// Other exception types will fall through.
    /// </summary>
    public static class Repeat
    {
        /// <summary>
        /// Catches an operation for retries.
        /// </summary>
        /// <param name="operation">The operation that might need retries</param>
        /// <returns>A handle for the retries.</returns>
        /// <remarks>This method does not by itself execute the operation. Another method must be chained to this call, e.g. <see cref="Times(RepeatChain, int)"/> or <see cref="UntilTrue"/></remarks>
        public static RepeatChain Operation(Action operation)
        {
            return new RepeatChain(operation);
        }

        /// <summary>
        /// Catches an operation for retries.
        /// </summary>
        /// <param name="operation">The operation that might need retries</param>
        /// <returns>A handle for the retries.</returns>
        /// <remarks>This method does not by itself execute the operation. Another method must be chained to this call, e.g. <see cref="Times(RepeatChain, int)"/> or <see cref="UntilTrue"/></remarks>
        public static RepeatChain Operation(Func<bool> operation)
        {
            return new RepeatChain(operation);
        }

        /// <summary>
        /// Repeats the operation at most <paramref name="numRetries"/> times.
        /// </summary>
        /// <param name="chain">The wrapped operation to perform</param>
        /// <param name="numRetries">The maximum number of retries</param>
        /// <returns>True if the operation succeeded with at most the given number of retries, false otherwise.</returns>
        public static bool Times(this RepeatChain chain, int numRetries)
        {
            return Times(chain, numRetries, TimeSpan.Zero);
        }

        /// <summary>
        /// Repeats the operation at most <paramref name="numRetries"/> times.
        /// </summary>
        /// <param name="chain">The wrapped operation to perform</param>
        /// <param name="numRetries">The maximum number of retries</param>
        /// <param name="timeBetweenRetries">Time between retries</param>
        /// <returns>True if the operation succeeded with at most the given number of retries, false otherwise.</returns>
        public static bool Times(this RepeatChain chain, int numRetries, TimeSpan timeBetweenRetries)
        {
            while (numRetries-- > 0)
            {
                try
                {
                    if (chain.Invoke())
                    {
                        return true;
                    }
                }
                catch (Exception e) when (IsRetryableException(e))
                {
                    // Ignore, retry
                    Thread.Sleep(timeBetweenRetries);
                }
            }

            return false;
        }

        public static void UntilTrue(this RepeatChain chain)
        {
            while (true)
            {
                try
                {
                    if (chain.Invoke())
                    {
                        return;
                    }
                }
                catch (Exception e) when (IsRetryableException(e))
                {
                    // Ignore, retry
                }
            }
        }

        /// <summary>
        /// Repeats the operation until it no longer throws a recoverable exception.
        /// </summary>
        /// <param name="chain">The wrapped operation</param>
        public static void UntilNoException(this RepeatChain chain)
        {
            while (true)
            {
                try
                {
                    chain.Invoke();
                    return;
                }
                catch (Exception e) when (IsRetryableException(e))
                {
                    // Ignore, retry
                }
            }
        }

        /// <summary>
        /// Repeats the operation until it no longer throws a recoverable exception.
        /// </summary>
        /// <param name="chain">The wrapped operation</param>
        /// <param name="timeOut">The maximum time to retry</param>
        public static bool UntilNoException(this RepeatChain chain, TimeSpan timeOut)
        {
            return UntilNoException(chain, timeOut, TimeSpan.Zero);
        }

        /// <summary>
        /// Repeats the operation until it no longer throws a recoverable exception.
        /// </summary>
        /// <param name="chain">The wrapped operation</param>
        /// <param name="timeOut">The maximum time to retry</param>
        /// <param name="timeBetweenRetries">Time between retries</param>
        public static bool UntilNoException(this RepeatChain chain, TimeSpan timeOut, TimeSpan timeBetweenRetries)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeOut)
            {
                try
                {
                    chain.Invoke();
                    return true;
                }
                catch (Exception e) when (IsRetryableException(e))
                {
                    // Ignore, retry
                    Thread.Sleep(timeBetweenRetries);
                }
            }

            return false;
        }

        private static bool IsRetryableException(Exception e)
        {
            return e is IOException || e is TimeoutException;
        }

        public class RepeatChain
        {
            // either one is always non-null
            private Action? _operation1;
            private Func<bool>? _operation2;

            internal RepeatChain(Action operation)
            {
                _operation1 = operation ?? throw new ArgumentNullException(nameof(operation));
            }

            internal RepeatChain(Func<bool> operation)
            {
                _operation2 = operation ?? throw new ArgumentNullException(nameof(operation));
            }

            internal bool Invoke()
            {
                if (_operation2 != null)
                {
                    return _operation2();
                }
                else
                {
                    _operation1?.Invoke();
                    return true;
                }
            }
        }
    }
}

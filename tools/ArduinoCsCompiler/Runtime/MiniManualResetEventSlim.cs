// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArduinoCsCompiler.Runtime
{
    /// <summary>
    /// Replaces ManualResetEventSlim basically with ManualResetEvent,
    /// because the latter is supported on nano, the earlier not.
    /// </summary>
    [ArduinoReplacement(typeof(ManualResetEventSlim), replaceEntireType: true, TargetFramework = TargetFramework.Nano)]
    internal class MiniManualResetEventSlim : IDisposable
    {
        private ManualResetEvent _event;

        public MiniManualResetEventSlim()
        {
            _event = new ManualResetEvent(false);
        }

        public MiniManualResetEventSlim(bool initialValue)
        {
            _event = new ManualResetEvent(initialValue);
        }

        public MiniManualResetEventSlim(bool initialValue, int spinCount)
        {
            _event = new ManualResetEvent(initialValue);
        }

        public bool IsSet => _event.WaitOne(0);

        public void Set()
        {
            _event.Set();
        }

        public void Reset()
        {
            _event.Reset();
        }

        public void WaitOne()
        {
            _event.WaitOne();
        }

        public void WaitOne(int millisecondsTimeout)
        {
            _event.WaitOne(millisecondsTimeout);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _event.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

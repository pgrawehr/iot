// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Arduino;

namespace ArduinoCsCompiler.Runtime
{
    /// <summary>
    /// This is a poor-mans implementation of <see cref="DependentHandle"/> for the nano framework, which doesn't support it.
    /// Unlike the correct implementation, this relies on someone calling a property to check if the target is still alive.
    /// </summary>
    [ArduinoReplacement(typeof(DependentHandle), true, TargetFramework = TargetFramework.Nano)]
    internal struct MiniDependentHandle2 : IDisposable
    {
        private WeakReference _target;
        private object? _dependent;
        private bool _disposed;

        public MiniDependentHandle2(Object? target, Object? dependent)
        {
            _target = new WeakReference(target);
            _dependent = dependent;
            _disposed = false;
        }

        public Object? Target
        {
            get
            {
                var t = _target.Target;
                if (t == null)
                {
                    _dependent = null;
                }

                return t;
            }
            set
            {
                _target.Target = value;
            }
        }

        public bool IsAllocated => !_disposed;

        public Object? Dependent
        {
            get
            {
                var t = _target.Target;
                if (t == null)
                {
                    _dependent = null;
                    return null;
                }

                return _dependent;
            }
            set
            {
                _dependent = value;
            }
        }

        public (object? Target, object? Dependent) TargetAndDependent
        {
            get
            {
                var d = _dependent;
                var t = _target.Target;
                if (t == null)
                {
                    _dependent = null;
                    return (null, null);
                }
                else
                {
                    return (t, d);
                }
            }
        }

        private object? InternalGetTargetAndDependent(out object? dependent)
        {
            var d = _dependent;
            var t = _target.Target;
            if (t == null)
            {
                dependent = null;
                return null;
            }
            else
            {
                dependent = d;
                return t;
            }
        }

        /// <summary>
        /// Gets the target object instance for the current handle.
        /// </summary>
        /// <returns>The target object instance, if present.</returns>
        /// <remarks>This method mirrors <see cref="Target"/>, but without the allocation check.</remarks>
        internal object? UnsafeGetTarget()
        {
            return Target;
        }

        /// <summary>
        /// Atomically retrieves the values of both <see cref="Target"/> and <see cref="Dependent"/>, if available.
        /// </summary>
        /// <param name="dependent">The dependent instance, if available.</param>
        /// <returns>The values of <see cref="Target"/> and <see cref="Dependent"/>.</returns>
        /// <remarks>
        /// This method mirrors the <see cref="TargetAndDependent"/> property, but without the allocation check.
        /// The signature is also kept the same as the one for the internal call, to improve the codegen.
        /// Note that <paramref name="dependent"/> is required to be on the stack (or it might not be tracked).
        /// </remarks>
        internal object? UnsafeGetTargetAndDependent(out object? dependent)
        {
            return InternalGetTargetAndDependent(out dependent);
        }

        /// <summary>
        /// Sets the dependent object instance for the current handle to <see langword="null"/>.
        /// </summary>
        /// <remarks>This method mirrors the <see cref="Target"/> setter, but without allocation and input checks.</remarks>
        internal void UnsafeSetTargetToNull()
        {
            Target = null;
            Dependent = null;
        }

        /// <summary>
        /// Sets the dependent object instance for the current handle.
        /// </summary>
        /// <remarks>This method mirrors <see cref="Dependent"/>, but without the allocation check.</remarks>
        internal void UnsafeSetDependent(object? dependent)
        {
            Dependent = dependent;
        }

        public void Dispose()
        {
            _target = new WeakReference(null);
            _dependent = null;
            _disposed = true;
        }
    }
}

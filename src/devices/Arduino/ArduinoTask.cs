using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1591
namespace Iot.Device.Arduino
{
    public interface IArduinoTask : IDisposable
    {
        MethodState State { get; }
        ArduinoMethodDeclaration MethodInfo { get; }
        void AddData(MethodState state, object[] args);
    }

    public sealed class ArduinoTask<T> : IArduinoTask, IDisposable
        where T : Delegate
    {
        private ConcurrentQueue<(MethodState, object[])> _collectedValues;
        internal ArduinoTask(T function, ArduinoCsCompiler compiler, ArduinoMethodDeclaration methodInfo)
        {
            Function = function;
            Compiler = compiler;
            MethodInfo = methodInfo;
            State = MethodState.Stopped;
            _collectedValues = new ConcurrentQueue<(MethodState, object[])>();
        }

        public T Function { get; }
        public ArduinoCsCompiler Compiler { get; }
        public ArduinoMethodDeclaration MethodInfo { get; }

        /// <summary>
        /// Returns the current state of the task
        /// </summary>
        public MethodState State
        {
            get;
            private set;
        }

        public void AddData(MethodState state, object[] args)
        {
            State = state;
            _collectedValues.Enqueue((state, args));
        }

        public void InvokeAsync(params int[] arguments)
        {
            if (State == MethodState.Running)
            {
                throw new InvalidOperationException("Task is already running");
            }

            State = MethodState.Running;
            Compiler.Invoke(MethodInfo.MethodInfo, arguments);
        }

        /// <summary>
        /// Returns a data set obtained from the realtime method.
        /// If this returns false, no data is available.
        /// If this returns true, the next data set is returned, together with the state of the task at that point.
        /// If the returned state is <see cref="MethodState.Stopped"/>, the data returned is the return value of the method.
        /// </summary>
        /// <param name="data">A set of values sent or returned by the task method</param>
        /// <param name="state">The state of the method matching the task at that time</param>
        /// <returns>True if data was available, false otherwise</returns>
        public bool GetMethodResults(out object[] data, out MethodState state)
        {
            if (_collectedValues.TryDequeue(out var d))
            {
                data = d.Item2;
                state = d.Item1;
                return true;
            }

            data = null;
            state = State;
            return false;
        }

        public void Dispose()
        {
            // Compiler.Delete(...)
        }
    }
}

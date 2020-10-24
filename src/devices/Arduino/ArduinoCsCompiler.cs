using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

#pragma warning disable CS1591
namespace Iot.Device.Arduino
{
    [Flags]
    public enum MethodFlags
    {
        None = 0,
        Static = 1,
        Virtual = 2,
        SpecialMethod = 4, // Method will resolve to a built-in function on the arduino
        Void = 8,
    }

    public enum MethodState
    {
        Stopped = 0,
        Aborted = 1,
        Running = 2,
    }

    public sealed class ArduinoCsCompiler : IDisposable
    {
        private readonly ArduinoBoard _board;
        private readonly Dictionary<MethodInfo, ArduinoMethodDeclaration> _methodInfos;
        private readonly List<IArduinoTask> _activeTasks;

        private int _numDeclaredMethods;

        public ArduinoCsCompiler(ArduinoBoard board)
        {
            _board = board;
            _numDeclaredMethods = 0;
            _methodInfos = new Dictionary<MethodInfo, ArduinoMethodDeclaration>();
            _board.SetCompilerCallback(BoardOnCompilerCallback);
            _activeTasks = new List<IArduinoTask>();
        }

        private string GetMethodName(ArduinoMethodDeclaration decl)
        {
            return decl.MethodInfo.Name;
        }

        internal void TaskDone(IArduinoTask task)
        {
            _activeTasks.Remove(task);
        }

        private void BoardOnCompilerCallback(int codeReference, MethodState state, object[] args)
        {
            var codeRef = _methodInfos.Values.FirstOrDefault(x => x.Index == codeReference);
            if (codeRef == null)
            {
                _board.Log($"Invalid method state message. Not currently knowing any method with reference {codeReference}.");
                return;
            }

            if (state == MethodState.Aborted)
            {
                _board.Log($"Execution of method {GetMethodName(codeRef)} caused an exception. Check previous messages.");
                return;
            }

            var task = _activeTasks.FirstOrDefault(x => x.MethodInfo == codeRef);
            if (task == null)
            {
                _board.Log($"Invalid method state update. {codeRef.Index} has no active task.");
                return;
            }

            if (state == MethodState.Stopped)
            {
                object retVal;
                int inVal = (int)args[0]; // initially, the list contains only ints
                // The method ended, therefore we know that the only element or args is the return value and can derive its correct type
                if (codeRef.MethodInfo.ReturnType == typeof(void))
                {
                    args = new object[0]; // Empty return set
                    task.AddData(state, args);
                    return;
                }
                else if (codeRef.MethodInfo.ReturnType == typeof(bool))
                {
                    retVal = inVal != 0;
                }
                else
                {
                    retVal = inVal;
                }

                args[0] = retVal;
            }

            task.AddData(state, args);
        }

        private byte[] GetIlCode(MethodInfo methodInstance)
        {
            var body = methodInstance.GetMethodBody();
            byte[] bytes = body.GetILAsByteArray();
            return bytes;
        }

        public ArduinoTask<T> LoadCode<T>(T method)
            where T : Delegate
        {
            return LoadCode(method, method.Method);
        }

        public void LoadLowLevelInterface()
        {
            Type lowLevelInterface = typeof(IArduinoHardwareLevelAccess);
            foreach (var method in lowLevelInterface.GetMethods())
            {
                if (!_methodInfos.ContainsKey(method))
                {
                    var attr = (ArduinoImplementationAttribute)method.GetCustomAttributes(typeof(ArduinoImplementationAttribute)).First();
                    ArduinoMethodDeclaration decl = new ArduinoMethodDeclaration(_numDeclaredMethods++, method.MetadataToken, method, MethodFlags.SpecialMethod, attr.MethodNumber);
                    _methodInfos.Add(method, decl);
                    LoadMethodDeclaration(decl);
                }
            }
        }

        private void LoadMethodDeclaration(ArduinoMethodDeclaration declaration)
        {
            _board.Firmata.SendMethodDeclaration((byte)declaration.Index, declaration.Token, declaration.Flags, (byte)Math.Max(declaration.MaxLocals, declaration.MaxStack), (byte)declaration.ArgumentCount);
        }

        private ArduinoTask<T> LoadCode<T>(T method, MethodInfo methodInfo)
            where T : Delegate
        {
            byte[] ilBytes = GetIlCode(methodInfo);
            if (ilBytes.Length > 255)
            {
                throw new InvalidProgramException($"Max IL size of real time method is 255. Actual size is {ilBytes.Length}.");
            }

            VerifyMethodCanBeLoaded(methodInfo);

            if (_methodInfos.ContainsKey(methodInfo))
            {
                // Nothing to do, already loaded
                var tsk = new ArduinoTask<T>(method, this, _methodInfos[methodInfo]);
                _activeTasks.Add(tsk);
                return tsk;
            }

            if (_numDeclaredMethods >= 255)
            {
                // In practice, the maximum will be much less on most Arduino boards
                throw new NotSupportedException("To many methods declared. Only 255 supported.");
            }

            var newInfo = new ArduinoMethodDeclaration(_numDeclaredMethods++, methodInfo);
            _methodInfos.Add(methodInfo, newInfo);

            LoadMethodDeclaration(newInfo);
            _board.Firmata.SendMethodIlCode((byte)newInfo.Index, ilBytes);

            var ret = new ArduinoTask<T>(method, this, newInfo);
            _activeTasks.Add(ret);
            return ret;
        }

        /// <summary>
        /// Executes the given method with the provided arguments asynchronously
        /// </summary>
        /// <remarks>Argument count/type not checked yet</remarks>
        /// <param name="method">Handle to method to invoke.</param>
        /// <param name="arguments">Argument list</param>
        internal void Invoke(MethodInfo method, params int[] arguments)
        {
            if (!_methodInfos.TryGetValue(method, out var decl))
            {
                throw new InvalidOperationException("Method must be loaded first.");
            }

            _board.Firmata.ExecuteIlCode((byte)decl.Index, arguments, method.ReturnType);
        }

        private void VerifyMethodCanBeLoaded(MethodInfo methodInstance)
        {
            if (methodInstance.ContainsGenericParameters)
            {
                throw new InvalidProgramException("No generics supported");
            }

            if (!methodInstance.IsStatic)
            {
                throw new InvalidProgramException("Only static methods supported");
            }

            if (methodInstance.GetMethodBody().ExceptionHandlingClauses.Count > 0)
            {
                throw new InvalidProgramException("Methods with exception handling are not supported");
            }

            // Check argument count, Check parameter types,
            // etc., etc.
        }

        public void Dispose()
        {
            _board.SetCompilerCallback(null);
        }
    }
}

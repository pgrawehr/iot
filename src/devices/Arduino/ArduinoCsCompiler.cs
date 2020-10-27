using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

#pragma warning disable CS1591
namespace Iot.Device.Arduino
{
    internal enum ExecutorCommand : byte
    {
        None = 0,
        DeclareMethod = 1,
        SetMethodTokens = 2,
        LoadIl = 3,
        StartTask = 4,
        ResetExecutor = 5,
        KillTask = 6,

        Nack = 0x7e,
        Ack = 0x7f,
    }

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
        Killed = 3,
    }

    public sealed class ArduinoCsCompiler : IDisposable
    {
        private readonly ArduinoBoard _board;
        private readonly Dictionary<MethodInfo, ArduinoMethodDeclaration> _methodInfos;
        private readonly List<IArduinoTask> _activeTasks;

        private int _numDeclaredMethods;

        public ArduinoCsCompiler(ArduinoBoard board, bool resetExistingCode = true)
        {
            _board = board;
            _numDeclaredMethods = 0;
            _methodInfos = new Dictionary<MethodInfo, ArduinoMethodDeclaration>();
            _board.SetCompilerCallback(BoardOnCompilerCallback);

            _activeTasks = new List<IArduinoTask>();

            if (resetExistingCode)
            {
                ClearAllData(true);
            }
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

            var task = _activeTasks.FirstOrDefault(x => x.MethodInfo == codeRef && x.State == MethodState.Running);

            if (task == null)
            {
                _board.Log($"Invalid method state update. {codeRef.Index} has no active task.");
                return;
            }

            if (state == MethodState.Aborted)
            {
                _board.Log($"Execution of method {GetMethodName(codeRef)} caused an exception. Check previous messages.");
                // Still update the task state, this will prevent a deadlock if somebody is waiting for this task to end
                task.AddData(state, new object[0]);
                return;
            }

            if (state == MethodState.Killed)
            {
                _board.Log($"Execution of method {GetMethodName(codeRef)} was forcibly terminated.");
                // Still update the task state, this will prevent a deadlock if somebody is waiting for this task to end
                task.AddData(state, new object[0]);
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

        private MemberInfo ResolveMember(MethodInfo method, int metadataToken)
        {
            Type type = method.DeclaringType;
            Type[] typeArgs = null, methodArgs = null;

            if (type.IsGenericType || type.IsGenericTypeDefinition)
            {
                typeArgs = type.GetGenericArguments();
            }

            if (method.IsGenericMethod || method.IsGenericMethodDefinition)
            {
                methodArgs = method.GetGenericArguments();
            }

            try
            {
                return type.Module.ResolveMember(metadataToken, typeArgs, methodArgs);
            }
            catch (ArgumentException)
            {
                // Due to our simplistic parsing below, we might find matching metadata tokens that aren't really tokens
                return null;
            }
        }

        public void LoadLowLevelInterface()
        {
            Type lowLevelInterface = typeof(IArduinoHardwareLevelAccess);
            foreach (var method in lowLevelInterface.GetMethods())
            {
                if (!_methodInfos.ContainsKey(method))
                {
                    var attr = (ArduinoImplementationAttribute)method.GetCustomAttributes(typeof(ArduinoImplementationAttribute)).First();
                    MemberInfo info = ResolveMember(method, 0x0A000072);

                    ArduinoMethodDeclaration decl = new ArduinoMethodDeclaration(_numDeclaredMethods++, method.MetadataToken, method, MethodFlags.SpecialMethod, attr.MethodNumber);
                    _methodInfos.Add(method, decl);
                    LoadMethodDeclaration(decl);
                }
            }

            // Also load the core methods
            LoadCode(new Action<IArduinoHardwareLevelAccess, int>(ArduinoRuntimeCore.Sleep));
        }

        private void LoadMethodDeclaration(ArduinoMethodDeclaration declaration)
        {
            _board.Firmata.SendMethodDeclaration((byte)declaration.Index, declaration.Token, declaration.Flags, (byte)Math.Max(declaration.MaxLocals, declaration.MaxStack), (byte)declaration.ArgumentCount);
        }

        private ArduinoTask<T> LoadCode<T>(T method, MethodInfo methodInfo)
            where T : Delegate
        {
            byte[] ilBytes = GetIlCode(methodInfo);
            List<int> foreignMethodTokensRequired = new List<int>();
            // Maps methodDef to memberRef tokens (for methods declared outside the assembly of the executing code)
            Dictionary<int, int> tokenMap = new Dictionary<int, int>();
            if (ilBytes.Length > 255)
            {
                throw new InvalidProgramException($"Max IL size of real time method is 255. Actual size is {ilBytes.Length}.");
            }

            if (_methodInfos.ContainsKey(methodInfo))
            {
                // Nothing to do, already loaded
                var tsk = new ArduinoTask<T>(method, this, _methodInfos[methodInfo]);
                _activeTasks.Add(tsk);
                return tsk;
            }

            VerifyMethodCanBeLoaded(methodInfo, foreignMethodTokensRequired);

            foreach (int token in foreignMethodTokensRequired.Distinct())
            {
                var resolved = ResolveMember(methodInfo, token);
                if (resolved == null)
                {
                    continue;
                }

                tokenMap.Add(resolved.MetadataToken, token);
            }

            if (_numDeclaredMethods >= 255)
            {
                // In practice, the maximum will be much less on most Arduino boards
                throw new NotSupportedException("To many methods declared. Only 255 supported.");
            }

            var newInfo = new ArduinoMethodDeclaration(_numDeclaredMethods++, methodInfo);
            _methodInfos.Add(methodInfo, newInfo);

            _board.Log($"Method Index {newInfo.Index} is named {methodInfo.Name}.");
            LoadMethodDeclaration(newInfo);
            LoadTokenMap((byte)newInfo.Index, tokenMap);
            _board.Firmata.SendMethodIlCode((byte)newInfo.Index, ilBytes);

            var ret = new ArduinoTask<T>(method, this, newInfo);
            _activeTasks.Add(ret);
            return ret;
        }

        private void LoadTokenMap(byte codeReference, Dictionary<int, int> tokenMap)
        {
            if (tokenMap.Count == 0)
            {
                return;
            }

            int[] data = new int[tokenMap.Count * 2];
            int idx = 0;
            foreach (var entry in tokenMap)
            {
                data[idx] = entry.Key;
                data[idx + 1] = entry.Value;
                idx += 2;
            }

            _board.Firmata.SendTokenMap(codeReference, data);
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

        public void KillTask(MethodInfo methodInfo)
        {
            if (!_methodInfos.TryGetValue(methodInfo, out var decl))
            {
                throw new InvalidOperationException("No such method known.");
            }

            _board.Firmata.SendKillTask((byte)decl.Index);
        }

        private void VerifyMethodCanBeLoaded(MethodInfo methodInstance, List<int> foreignMethodTokensRequired)
        {
            if (methodInstance.ContainsGenericParameters)
            {
                throw new InvalidProgramException("No generics supported");
            }

            if (!methodInstance.IsStatic)
            {
                throw new InvalidProgramException("Only static methods supported");
            }

            MethodBody body = methodInstance.GetMethodBody();
            if (body == null)
            {
                throw new InvalidProgramException($"Method {methodInstance.Name} has no implementation.");
            }

            if (body.ExceptionHandlingClauses.Count > 0)
            {
                throw new InvalidProgramException("Methods with exception handling are not supported");
            }

            // TODO: Check argument count, Check parameter types, etc., etc.
            byte[] byteCode = body.GetILAsByteArray();
            // TODO: This is very simplistic so we do not need another parser. But this might have false positives
            int idx = 0;
            while (idx < byteCode.Length - 5)
            {
                // Decode token first (number is little endian!)
                int token = byteCode[idx + 1] | byteCode[idx + 2] << 8 | byteCode[idx + 3] << 16 | byteCode[idx + 4] << 24;
                if ((byteCode[idx] == 0x6F || byteCode[idx] == 0x28) && (token >> 24 == 0x0A))
                {
                    // The tokens we're interested in have the form 0x0A XX XX XX preceded by a call or callvirt instruction
                    foreignMethodTokensRequired.Add(token);
                }

                idx++;
            }
        }

        /// <summary>
        /// Clears all execution data from the arduino, so that the memory is freed again.
        /// </summary>
        /// <param name="force">True to also kill the current task. If false and code is being executed, nothing happens.</param>
        public void ClearAllData(bool force)
        {
            _board.Firmata.SendIlResetCommand(force);
            _numDeclaredMethods = 0;
            _activeTasks.Clear();
            _methodInfos.Clear();
        }

        public void Dispose()
        {
            _board.SetCompilerCallback(null);
        }
    }
}

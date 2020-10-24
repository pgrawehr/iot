using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

#pragma warning disable CS1591
namespace Iot.Device.Arduino
{
    [Flags]
    internal enum MethodFlags
    {
        None = 0,
        Static = 1,
        Virtual = 2,
        SpecialMethod = 4, // Method will resolve to a built-in function on the arduino
        Void = 8,
    }

    public class ArduinoCsCompiler
    {
        private readonly ArduinoBoard _board;
        private readonly Dictionary<MethodInfo, ArduinoMethodDecl> _methodInfos;

        private int _numDeclaredMethods;

        public ArduinoCsCompiler(ArduinoBoard board)
        {
            _board = board;
            _numDeclaredMethods = 0;
            _methodInfos = new Dictionary<MethodInfo, ArduinoMethodDecl>();
        }

        private byte[] GetIlCode(MethodInfo methodInstance)
        {
            var body = methodInstance.GetMethodBody();
            byte[] bytes = body.GetILAsByteArray();
            return bytes;
        }

        public MethodInfo LoadCode(Delegate method)
        {
            return LoadCode(method.Method);
        }

        public ArduinoTask<T> LoadCode<T>(T method)
            where T : Delegate
        {
            return LoadCode(method.Method);
        }

        public void LoadLowLevelInterface()
        {
            Type lowLevelInterface = typeof(IArduinoHardwareLevelAccess);
            foreach (var method in lowLevelInterface.GetMethods())
            {
                if (!_methodInfos.ContainsKey(method))
                {
                    var attr = (ArduinoImplementationAttribute)method.GetCustomAttributes(typeof(ArduinoImplementationAttribute)).First();
                    ArduinoMethodDecl decl = new ArduinoMethodDecl(_numDeclaredMethods++, method.MetadataToken, method, MethodFlags.SpecialMethod, attr.MethodNumber);
                    _methodInfos.Add(method, decl);
                    LoadMethodDeclaration(decl);
                }
            }
        }

        private void LoadMethodDeclaration(ArduinoMethodDecl declaration)
        {
            _board.Firmata.SendMethodDeclaration((byte)declaration.Index, declaration.Token, declaration.Flags, (byte)Math.Max(declaration.MaxLocals, declaration.MaxStack), (byte)declaration.ArgumentCount);
        }

        private MethodInfo LoadCode(MethodInfo method)
        {
            byte[] ilBytes = GetIlCode(method);
            if (ilBytes.Length > 255)
            {
                throw new InvalidProgramException($"Max IL size of real time method is 255. Actual size is {ilBytes.Length}.");
            }

            VerifyMethodCanBeLoaded(method);

            if (_methodInfos.ContainsKey(method))
            {
                // Nothing to do, already loaded
                return method;
            }

            if (_numDeclaredMethods >= 255)
            {
                // In practice, the maximum will be much less on most Arduino boards
                throw new NotSupportedException("To many methods declared. Only 255 supported.");
            }

            var newInfo = new ArduinoMethodDecl(_numDeclaredMethods++, method);
            _methodInfos.Add(method, newInfo);

            LoadMethodDeclaration(newInfo);
            _board.Firmata.SendMethodIlCode((byte)newInfo.Index, ilBytes);

            return method;
        }

        /// <summary>
        /// Executes the given method with the provided arguments.
        /// If the method being called has return type void, the execution is started asynchronously, otherwise,
        /// the result is waited for.
        /// </summary>
        /// <remarks>Argument count/type not checked yet</remarks>
        /// <param name="method">Handle to method to invoke.</param>
        /// <param name="arguments">Argument list</param>
        /// <returns>The return value of the indicated method, null for void methods.</returns>
        public object Invoke(MethodInfo method, params int[] arguments)
        {
            if (!_methodInfos.TryGetValue(method, out var decl))
            {
                throw new InvalidOperationException("Method must be loaded first.");
            }

            int returned = _board.Firmata.ExecuteIlCodeSynchronous((byte)decl.Index, arguments, method.ReturnType);
            if (method.ReturnType == typeof(void))
            {
                return null;
            }
            else if (method.ReturnType == typeof(int))
            {
                return returned;
            }
            else if (method.ReturnType == typeof(bool))
            {
                return (returned != 0);
            }

            // TODO: Extend
            return returned;
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

        private sealed class ArduinoMethodDecl
        {
            public ArduinoMethodDecl(int index, MethodInfo methodInfo)
            {
                Index = index;
                MethodInfo = methodInfo;
                Flags = MethodFlags.None;
                var body = methodInfo.GetMethodBody();
                Token = methodInfo.MetadataToken;
                MaxLocals = body.LocalVariables.Count;
                MaxStack = body.MaxStackSize;
                ArgumentCount = methodInfo.GetParameters().Length;
                if (methodInfo.CallingConvention.HasFlag(CallingConventions.HasThis))
                {
                    ArgumentCount += 1;
                }

                if (methodInfo.IsStatic)
                {
                    Flags |= MethodFlags.Static;
                }

                if (methodInfo.IsVirtual)
                {
                    Flags |= MethodFlags.Virtual;
                }

                if (methodInfo.ReturnParameter == null)
                {
                    Flags |= MethodFlags.Void;
                }
            }

            public ArduinoMethodDecl(int index, int token, MethodInfo methodInfo, MethodFlags flags, int maxStack)
            {
                Index = index;
                Token = token;
                MethodInfo = methodInfo;
                Flags = flags;
                MaxLocals = MaxStack = maxStack;
                ArgumentCount = methodInfo.GetParameters().Length;
                if (methodInfo.CallingConvention.HasFlag(CallingConventions.HasThis))
                {
                    ArgumentCount += 1;
                }

                if (methodInfo.ReturnParameter.ParameterType == typeof(void))
                {
                    Flags |= MethodFlags.Void;
                }
            }

            public int Index { get; }
            public int Token { get; }
            public MethodInfo MethodInfo { get; }

            public MethodFlags Flags
            {
                get;
            }

            public int MaxLocals
            {
                get;
            }

            public int MaxStack
            {
                get;
            }

            public int ArgumentCount
            {
                get;
            }
        }
    }
}

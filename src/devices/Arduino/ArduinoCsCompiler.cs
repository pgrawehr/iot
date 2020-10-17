using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

#pragma warning disable CS1591
namespace Iot.Device.Arduino
{
    public class ArduinoCsCompiler
    {
        private readonly ArduinoBoard _board;

        public ArduinoCsCompiler(ArduinoBoard board)
        {
            _board = board;
        }

        private byte[] GetIlCode(MethodInfo methodInstance)
        {
            // MethodInfo methodInstance = typeof(ArduinoCompilerMethods).GetMethod(nameof(ArduinoCompilerMethods.AddInts));
            var body = methodInstance.GetMethodBody();
            byte[] bytes = body.GetILAsByteArray();
            return bytes;
        }

        public void LoadCode(Func<int, int, int> method)
        {
            LoadCode(method.Method);
        }

        public void LoadCode(Func<int, int, bool> method)
        {
            LoadCode(method.Method);
        }

        private void LoadCode(MethodInfo method)
        {
            byte[] ilBytes = GetIlCode(method);
            if (ilBytes.Length > 255)
            {
                throw new InvalidProgramException($"Max IL size of real time method is 255. Actual size is {ilBytes.Length}.");
            }

            VerifyMethodCanBeLoaded(method);

            _board.Firmata.SendMethodIlCode(0, ilBytes);
        }

        public int ExecuteCode(params int[] arguments)
        {
            // VERY simple case: We support only exactly two int parameters right now
            int[] parameters =
            {
                arguments[0], arguments[1]
            };

            return _board.Firmata.ExecuteIlCodeSynchronous(0, parameters);
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

            // Check argument count, Check parameter types,
            // etc., etc.
        }
    }
}

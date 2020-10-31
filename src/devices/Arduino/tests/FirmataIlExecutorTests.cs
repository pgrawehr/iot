using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Iot.Device.Arduino.Tests
{
    public class FirmataIlExecutorTests : IDisposable
    {
        private const String PortName = "COM4";
        private SerialPort _serialPort;
        private ArduinoBoard _board;
        private ArduinoCsCompiler _compiler;

        public FirmataIlExecutorTests()
        {
            _serialPort = new SerialPort(PortName, 115200);
            _serialPort.Open();
            _board = new ArduinoBoard(_serialPort.BaseStream);
            _board.Initialize();
            _compiler = new ArduinoCsCompiler(_board, true);
        }

        public void Dispose()
        {
            _compiler.ClearAllData(true);
            _board.Dispose();
            _serialPort.Dispose();
        }

        public static int AddInts(int a, int b)
        {
            return a + b;
        }

        public static int SubtactInts(int a, int b)
        {
            return a - b;
        }

        public static bool Equal(int a, int b)
        {
            return a == b;
        }

        public static bool Not(int a, int b)
        {
            return !(a == b);
        }

        public static bool Unequal(int a, int b)
        {
            return (a != b);
        }

        public static bool Smaller(int a, int b)
        {
            return a < b;
        }

        public static bool SmallerOrEqual(int a, int b)
        {
            return a <= b;
        }

        public static bool Greater(int a, int b)
        {
            return a > b;
        }

        public static bool GreaterOrEqual(int a, int b)
        {
            return a >= b;
        }

        public static bool GreaterThanConstant(int a, int b)
        {
            return a > 2700;
        }

        private void LoadCodeMethod(Type type, string methodName, int a, int b, bool expectedResult)
        {
            var methods = type.GetMethods().Where(x => x.Name == methodName).ToList();
            Assert.Single(methods);
            _compiler.LoadLowLevelInterface();
            CancellationTokenSource cs = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var method = _compiler.LoadCode<Func<int, int, bool>>(methods[0]);

            // This assertion fails on a timeout
            Assert.True(method.Invoke(cs.Token, a, b));

            // The task has terminated
            Assert.Equal(MethodState.Stopped, method.State);

            Assert.True(method.GetMethodResults(out object[] data, out MethodState state));
            // The only result is from the end of the method
            Assert.Equal(MethodState.Stopped, state);
            Assert.Single(data);

            bool result = (bool)data[0];
            Assert.Equal(expectedResult, result);
            method.Dispose();
        }

        [Theory]
        [InlineData("Equal", 2, 2, true)]
        [InlineData("Equal", 2000, 1999, false)]
        [InlineData("Equal", -1, -1, true)]
        [InlineData("Smaller", 1, 2, true)]
        [InlineData("SmallerOrEqual", 7, 20, true)]
        [InlineData("SmallerOrEqual", 7, 7, true)]
        [InlineData("SmallerOrEqual", 21, 7, false)]
        [InlineData("Greater", 0x12345678, 0x12345677, true)]
        [InlineData("GreaterOrEqual", 2, 2, true)]
        [InlineData("GreaterOrEqual", 787878, 787877, true)]
        [InlineData("GreaterThanConstant", 2701, 0, true)]
        ////[InlineData("Smaller", -1, 1, true)] // TODO: Only unsigned types supported
        ////[InlineData("Unequal", 2, 2, false)]
        ////[InlineData("SmallerOrEqual", -2, -1, true)]
        ////[InlineData("SmallerOrEqual", -2, -2, false)]
        public void TestBooleanOperation(string methodName, int argument1, int argument2, bool expected)
        {
            LoadCodeMethod(GetType(), methodName, argument1, argument2, expected);
        }
    }
}

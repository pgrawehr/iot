// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
using System.Device.I2c;
using Board.Tests;
using Moq;
using Xunit;

namespace Iot.Device.Board.Tests
{
    public class BoardTests : IDisposable
    {
        private Mock<MockableGpioDriver> _mockedGpioDriver;

        public BoardTests()
        {
            _mockedGpioDriver = new Mock<MockableGpioDriver>(MockBehavior.Default);
            _mockedGpioDriver.CallBase = true;
        }

        public void Dispose()
        {
            _mockedGpioDriver.VerifyAll();
        }

        [Fact]
        public void ThereIsAlwaysAMatchingBoard()
        {
            // This should always return something valid, and be it only something with an empty controller
            var board = Board.DetermineOptimalBoardForHardware(PinNumberingScheme.Logical);
            Assert.NotNull(board);
            board.Initialize();
            Assert.Equal(PinNumberingScheme.Logical, board.DefaultPinNumberingScheme);
            board.Dispose();
        }

        [Fact]
        public void GpioControllerCreateOpenClosePin()
        {
            var board = CreateBoard();
            _mockedGpioDriver.Setup(x => x.OpenPinEx(1));
            _mockedGpioDriver.Setup(x => x.IsPinModeSupportedEx(1, PinMode.Output)).Returns(true);
            _mockedGpioDriver.Setup(x => x.GetPinModeEx(1)).Returns(PinMode.Output);
            _mockedGpioDriver.Setup(x => x.WriteEx(1, PinValue.High));
            _mockedGpioDriver.Setup(x => x.ClosePinEx(1));
            var ctrl = board.CreateGpioController(null);
            Assert.NotNull(ctrl);
            ctrl.OpenPin(1, PinMode.Output);
            ctrl.Write(1, PinValue.High);
            ctrl.ClosePin(1);
        }

        [Fact]
        public void OpenPinNotAssignedThrows()
        {
            var board = CreateBoard();
            var ctrl = board.CreateGpioController(new int[] { 2 });
            Assert.Throws<InvalidOperationException>(() => ctrl.OpenPin(1));
            Assert.Throws<InvalidOperationException>(() => ctrl.ClosePin(1));
        }

        [Fact]
        public void OpenPinAlreadyAssignedThrows()
        {
            var board = CreateBoard();
            var ctrl = board.CreateGpioController();
            ctrl.OpenPin(1);
            Assert.Throws<InvalidOperationException>(() => ctrl.OpenPin(1));
        }

        [Fact]
        public void OpenPinAlreadyAssignedToOtherControllerThrows()
        {
            var board = CreateBoard();
            var ctrl = board.CreateGpioController();
            ctrl.OpenPin(1);
            var ctrl2 = board.CreateGpioController(); // This so far is valid
            Assert.Throws<InvalidOperationException>(() => ctrl2.OpenPin(1));
        }

        [Fact]
        public void UsingBoardNumberingWorks()
        {
            _mockedGpioDriver.Setup(x => x.OpenPinEx(1));
            _mockedGpioDriver.Setup(x => x.SetPinModeEx(1, PinMode.Output));
            _mockedGpioDriver.Setup(x => x.IsPinModeSupportedEx(1, PinMode.Output)).Returns(true);
            _mockedGpioDriver.Setup(x => x.GetPinModeEx(1)).Returns(PinMode.Output);
            _mockedGpioDriver.Setup(x => x.WriteEx(1, PinValue.High));
            _mockedGpioDriver.Setup(x => x.ReadEx(1)).Returns(PinValue.High);
            _mockedGpioDriver.Setup(x => x.ClosePinEx(1));
            Board b = new CustomGenericBoard(PinNumberingScheme.Board) { MockedDriver = _mockedGpioDriver.Object };
            var ctrl = b.CreateGpioController();
            ctrl.OpenPin(2, PinMode.Output); // Our test board maps physical pin 2 to logical pin 1
            ctrl.Write(2, PinValue.High);
            Assert.Equal(PinValue.High, ctrl.Read(2));
            ctrl.ClosePin(2);
        }

        [Fact]
        public void UsingBoardNumberingForCallbackWorks()
        {
            _mockedGpioDriver.Setup(x => x.OpenPinEx(1));
            _mockedGpioDriver.Setup(x => x.AddCallbackForPinValueChangedEventEx(1,
                PinEventTypes.Rising, It.IsAny<PinChangeEventHandler>()));
            Board b = new CustomGenericBoard(PinNumberingScheme.Board) { MockedDriver = _mockedGpioDriver.Object };
            var ctrl = b.CreateGpioController();
            ctrl.OpenPin(2); // logical pin 1 on our test board
            ctrl.RegisterCallbackForPinValueChangedEvent(2, PinEventTypes.Rising, (sender, args) =>
            {
                Assert.Equal(1, args.PinNumber);
            });
        }

        [Fact]
        public void UsingMultiplePinsWorks()
        {
            _mockedGpioDriver.Setup(x => x.OpenPinEx(1));
            _mockedGpioDriver.Setup(x => x.IsPinModeSupportedEx(1, PinMode.Output)).Returns(true);
            Board b = new CustomGenericBoard(PinNumberingScheme.Logical) { MockedDriver = _mockedGpioDriver.Object };
            var ctrl = b.CreateGpioController(new int[] { 2, 4, 8 }, PinNumberingScheme.Board);
            ctrl.OpenPin(2, PinMode.Output); // Our test board maps physical pin 2 to logical pin 1
        }

        [Fact]
        public void ReservePin()
        {
            var board = CreateBoard();
            board.ReservePin(1, PinUsage.I2c, this);
            // Already in use for I2c
            Assert.Throws<InvalidOperationException>(() => board.ReservePin(1, PinUsage.Gpio, this));
            // Already in use
            Assert.Throws<InvalidOperationException>(() => board.ReservePin(1, PinUsage.I2c, this));
        }

        [Fact]
        public void ReserveReleasePin()
        {
            var board = CreateBoard();
            board.ReservePin(1, PinUsage.I2c, this);
            // Not reserved for Gpio
            Assert.Throws<InvalidOperationException>(() => board.ReleasePin(1, PinUsage.Gpio, this));
            // Reserved by somebody else
            Assert.Throws<InvalidOperationException>(() => board.ReleasePin(1, PinUsage.I2c, new object()));
            // Not reserved
            Assert.Throws<InvalidOperationException>(() => board.ReleasePin(2, PinUsage.Pwm, this));

            board.ReleasePin(1, PinUsage.I2c, this);
        }

        [Fact]
        public void CreateI2cDeviceDefault()
        {
            var board = CreateBoard();
            var device = board.CreateI2cDevice(new I2cConnectionSettings(0, 3)) as I2cDummyDevice;
            Assert.NotNull(device);
            Assert.Equal(0, device.PinAssignment[0]);
            Assert.Equal(1, device.PinAssignment[1]);
        }

        [Fact]
        public void CreateI2cDeviceBoardNumbering()
        {
            var board = CreateBoard();
            var device = board.CreateI2cDevice(new I2cConnectionSettings(0, 3), new int[] { 2, 4 }, PinNumberingScheme.Board) as I2cDummyDevice;
            Assert.NotNull(device);
            Assert.Equal(1, device.PinAssignment[0]);
            Assert.Equal(2, device.PinAssignment[1]);
        }

        private Board CreateBoard()
        {
            return new CustomGenericBoard(PinNumberingScheme.Logical) { MockedDriver = _mockedGpioDriver.Object };
        }

        private sealed class CustomGenericBoard : GenericBoard
        {
            public CustomGenericBoard(PinNumberingScheme numberingScheme)
                : base(numberingScheme)
            {
            }

            public GpioDriver MockedDriver
            {
                get;
                set;
            }

            public override int ConvertPinNumber(int pinNumber, PinNumberingScheme inputScheme, PinNumberingScheme outputScheme)
            {
                if (inputScheme == PinNumberingScheme.Logical && outputScheme == PinNumberingScheme.Board)
                {
                    return pinNumber switch
                    {
                        1 => 2,
                        2 => 4,
                        4 => 8,
                        _ => base.ConvertPinNumber(pinNumber, inputScheme, outputScheme)
                    };
                }
                else if (inputScheme == PinNumberingScheme.Board && outputScheme == PinNumberingScheme.Logical)
                {
                    return pinNumber switch
                    {
                        2 => 1,
                        4 => 2,
                        8 => 4,
                        _ => base.ConvertPinNumber(pinNumber, inputScheme, outputScheme)
                    };
                }

                return base.ConvertPinNumber(pinNumber, inputScheme, outputScheme);
            }

            /// <summary>
            /// Overridden, because driver implementation currently does not support mocking
            /// </summary>
            public override AlternatePinMode GetHardwareModeForPinUsage(int pinNumber, PinUsage usage, PinNumberingScheme pinNumberingScheme = PinNumberingScheme.Logical, int bus = 0)
            {
                if (usage == PinUsage.Gpio)
                {
                    return AlternatePinMode.Gpio;
                }

                return base.GetHardwareModeForPinUsage(pinNumber, usage, pinNumberingScheme, bus);
            }

            protected override GpioDriver CreateDriver()
            {
                return MockedDriver;
            }

            public override int[] GetDefaultPinAssignmentForI2c(I2cConnectionSettings connectionSettings)
            {
                if (connectionSettings.BusId == 0)
                {
                    return new int[] { 0, 1 };
                }

                throw new NotSupportedException($"No simulated bus id {connectionSettings.BusId}");
            }

            public override I2cDevice CreateI2cDevice(I2cConnectionSettings connectionSettings, int[] pins, PinNumberingScheme pinNumberingScheme)
            {
                return new I2cDummyDevice(connectionSettings, RemapPins(pins, pinNumberingScheme));
            }
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
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
            var ctrl = board.CreateGpioController();
            Assert.NotNull(ctrl);
            ctrl.OpenPin(1, PinMode.Output);
            ctrl.Write(1, PinValue.High);
            ctrl.ClosePin(1);
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
            var ctrl = b.CreateGpioController(PinNumberingScheme.Board);
            ctrl.OpenPin(2, PinMode.Output); // Our test board maps physical pin 2 to logical pin 1
        }

        [Fact]
        public void ReservePinI2c()
        {
            var board = CreateBoard();
            board.ReservePin(1, PinUsage.I2c, this);
            // Already in use for I2c
            Assert.Throws<InvalidOperationException>(() => board.ReservePin(1, PinUsage.Gpio, this));
            // Works, I2c can share pins
            board.ReservePin(1, PinUsage.I2c, this);
        }

        [Fact]
        public void ReservePinGpio()
        {
            var board = CreateBoard();
            board.ReservePin(1, PinUsage.Gpio, this);
            // Already in use for Gpio
            Assert.Throws<InvalidOperationException>(() => board.ReservePin(1, PinUsage.I2c, this));
            // Fails, Gpio cannot share pins
            Assert.Throws<InvalidOperationException>(() => board.ReservePin(1, PinUsage.Gpio, this));
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
            var device = board.CreateI2cDevice(new I2cConnectionSettings(0, 3)) as I2cDeviceManager;
            Assert.NotNull(device);
            var simDevice = device.RawDevice as I2cDummyDevice;
            Assert.Equal(0, simDevice.Pins[0]);
            Assert.Equal(1, simDevice.Pins[1]);
        }

        [Fact]
        public void CreateI2cDeviceBoardNumbering()
        {
            var board = CreateBoard();
            var device = board.CreateI2cDevice(new I2cConnectionSettings(0, 3), new int[] { 2, 4 }, PinNumberingScheme.Board) as I2cDeviceManager;
            Assert.NotNull(device);
            var simDevice = device.RawDevice as I2cDummyDevice;
            Assert.NotNull(simDevice);
            Assert.Equal(1, simDevice.Pins[0]);
            Assert.Equal(2, simDevice.Pins[1]);
        }

        [Fact]
        public void TwoI2cDevicesCanSharePins()
        {
            var board = CreateBoard();
            var device1 = board.CreateI2cDevice(new I2cConnectionSettings(0, 1));
            var device2 = board.CreateI2cDevice(new I2cConnectionSettings(0, 2));
            // Now all fine
            Assert.Equal(0xff, device1.ReadByte());
            Assert.Equal(0xff, device2.ReadByte());
            Assert.Equal(PinUsage.I2c, board.DetermineCurrentPinUsage(0));
            Assert.Equal(PinUsage.I2c, board.DetermineCurrentPinUsage(1));
            device1.Dispose();

            // Still fine
            Assert.Equal(0xff, device2.ReadByte());
            // Not so fine
            Assert.Throws<ObjectDisposedException>(() => device1.ReadByte());
            // Also not fine (since pins still open)
            var ctrl = board.CreateGpioController();
            Assert.Throws<InvalidOperationException>(() => ctrl.OpenPin(0));
            device2.Dispose();
            // Now fine
            ctrl.OpenPin(0);
        }

        [Fact]
        public void CreateSpiDeviceDefault()
        {
            var board = CreateBoard();
            var device = board.CreateSpiDevice(new SpiConnectionSettings(0, 0)) as SpiDeviceManager;
            Assert.NotNull(device);
            var simDevice = device.RawDevice as SpiDummyDevice;
            Assert.NotNull(simDevice);
            Assert.Equal(0xF8, simDevice.ReadByte());
            // See simulation board implementation why this should be the case
            Assert.Equal(new int[] { 2, 3, 4, 10 }, simDevice.Pins);
        }

        [Fact]
        public void TwoSpiDevicesCanSharePins()
        {
            var board = CreateBoard();
            var device1 = board.CreateSpiDevice(new SpiConnectionSettings(0, 1));
            var device2 = board.CreateSpiDevice(new SpiConnectionSettings(0, 2));
            // Now all fine
            Assert.Equal(0xf8, device1.ReadByte());
            Assert.Equal(0xf8, device2.ReadByte());
            Assert.Equal(PinUsage.Spi, board.DetermineCurrentPinUsage(2));
            Assert.Equal(PinUsage.Spi, board.DetermineCurrentPinUsage(3));
            device1.Dispose();

            // Still fine
            Assert.Equal(0xf8, device2.ReadByte());
            // Not so fine
            Assert.Throws<ObjectDisposedException>(() => device1.ReadByte());
            // Also not fine (since pins still open)
            var ctrl = board.CreateGpioController();
            Assert.Throws<InvalidOperationException>(() => ctrl.OpenPin(2));
            device2.Dispose();
            // Now fine
            ctrl.OpenPin(0);
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

            public override int[] GetDefaultPinAssignmentForSpi(SpiConnectionSettings connectionSettings)
            {
                if (connectionSettings.BusId == 0)
                {
                    if (connectionSettings.ChipSelectLine == 0 || connectionSettings.ChipSelectLine == -1)
                    {
                        return new int[]
                        {
                            2, 3, 4, 10 // simulate: CE0 is logical pin 10
                        };
                    }
                    else
                    {
                        return new int[] { 2, 3, 4 };
                    }
                }

                throw new NotSupportedException($"No simulated bus id {connectionSettings.BusId}");
            }

            protected override I2cDevice CreateSimpleI2cDevice(I2cConnectionSettings connectionSettings, int[] pins)
            {
                return new I2cDummyDevice(connectionSettings, pins);
            }

            protected override SpiDevice CreateSimpleSpiDevice(SpiConnectionSettings connectionSettings, int[] pins)
            {
                return new SpiDummyDevice(connectionSettings, pins);
            }
        }
    }
}
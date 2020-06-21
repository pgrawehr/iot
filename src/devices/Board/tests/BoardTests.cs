// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
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
            _mockedGpioDriver.Setup(x => x.IsPinModeSupportedEx(1, PinMode.Output)).Returns(true);
            Board b = new CustomGenericBoard(PinNumberingScheme.Board) { MockedDriver = _mockedGpioDriver.Object };
            var ctrl = b.CreateGpioController();
            ctrl.OpenPin(2); // Our test board maps physical pin 2 to logical pin 1
            ctrl.Write(2, PinValue.High);
            Assert.Equal(PinValue.High, ctrl.Read(2));
            ctrl.ClosePin(2);
        }

        [Fact]
        public void UsingBoardNumberingForCallbackWorks()
        {
            Board b = new CustomGenericBoard(PinNumberingScheme.Board) { MockedDriver = _mockedGpioDriver.Object };
            var ctrl = b.CreateGpioController();
            ctrl.OpenPin(1);
            ctrl.RegisterCallbackForPinValueChangedEvent(1, PinEventTypes.Rising, (sender, args) =>
            {
                Assert.Equal(1, args.PinNumber);
            });
        }

        /// <summary>
        /// This shouldn't map anything in either direction
        /// </summary>
        [Fact]
        public void PinMappingIsReversibleLogical()
        {
            RaspberryPiBoard b = new RaspberryPiBoard(PinNumberingScheme.Logical);
            for (int i = 0; i < b.PinCount; i++)
            {
                int mapped = b.ConvertPinNumber(i, PinNumberingScheme.Logical, PinNumberingScheme.Logical);
                int reverse = b.ConvertPinNumber(i, PinNumberingScheme.Logical, PinNumberingScheme.Logical);
                Assert.Equal(reverse, mapped);
                Assert.Equal(mapped, i);
            }
        }

        /// <summary>
        /// The mapping is not 1:1, but must be reversible
        /// </summary>
        [Fact]
        public void PinMappingIsReversibleBoard()
        {
            RaspberryPiBoard b = new RaspberryPiBoard(PinNumberingScheme.Board);
            Assert.NotEqual(3, b.ConvertPinNumber(3, PinNumberingScheme.Board, PinNumberingScheme.Logical));
            for (int i = 0; i < b.PinCount; i++)
            {
                int mapped = b.ConvertPinNumber(i, PinNumberingScheme.Logical, PinNumberingScheme.Board);
                int reverse = b.ConvertPinNumber(mapped, PinNumberingScheme.Board, PinNumberingScheme.Logical);
                Assert.Equal(i, reverse);
            }
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
        }
    }
}

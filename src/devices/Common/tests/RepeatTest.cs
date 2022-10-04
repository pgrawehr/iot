// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Iot.Device.Common;

namespace Common.Tests
{
    public class RepeatTest
    {
        [Fact]
        public void Times()
        {
            Assert.False(Repeat.Operation(() => throw new IOException()).Times(3));
        }

        [Fact]
        public void UntilTrue()
        {
            Repeat.Operation(() => true).UntilTrue();
        }

        [Fact]
        public void UntilNoExceptionWithTimeout()
        {
            Assert.False(Repeat.Operation(DoSomethingThatFails).UntilNoException(TimeSpan.FromSeconds(1)));
        }

        private void DoSomethingThatFails()
        {
            throw new IOException("There was an error");
        }
    }
}

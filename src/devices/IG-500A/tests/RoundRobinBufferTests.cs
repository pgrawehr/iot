// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Iot.Device.Imu.Tests
{
    public class RoundRobinBufferTests
    {
        [Fact]
        public void InsertingAndRetrievingDataWorks()
        {
            var r = new RoundRobinBuffer(100);
            byte[] dataSet = new byte[9] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] outBuffer = new byte[9];
            r.InsertBytes(dataSet, 9);
            Assert.Equal(9, r.GetBuffer(outBuffer, 21));
            r.ConsumeBytes(9);
            Assert.True(r.BytesInBuffer == 0);
            Assert.Equal(dataSet, outBuffer);
        }

        [Fact]
        public void DataIsCorrectlyConsumed()
        {
            var r = new RoundRobinBuffer(100);
            byte[] dataSet = new byte[9] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] outBuffer = new byte[9];
            r.InsertBytes(dataSet, 9);
            Assert.Equal(2, r.GetBuffer(outBuffer, 2));
            r.ConsumeBytes(2);
            Assert.True(r.BytesInBuffer == 7);
            Assert.Equal(1, outBuffer[0]);
            Assert.Equal(1, r.GetBuffer(outBuffer, 1));
            Assert.Equal(3, outBuffer[0]);
        }

        [Fact]
        public void OverflowWorksCorrectlyOddLength()
        {
            var r = new RoundRobinBuffer(100);
            byte[] dataSet = new byte[9] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            for (int i = 0; i < 12; i++)
            {
                r.InsertBytes(dataSet, 9);
                byte[] outBuffer = new byte[9];
                Assert.Equal(9, r.GetBuffer(outBuffer, 9));
                Assert.Equal(dataSet, outBuffer);
                r.ConsumeBytes(9);
            }

        }

        [Fact]
        public void OverflowWorksCorrectlyEvenLength()
        {
            var r = new RoundRobinBuffer(100);
            // The dataset size is a fraction of the buffer length, such that we once hit exactly the case were the block index equals the buffer length
            byte[] dataSet = new byte[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            for (int i = 0; i < 10; i++)
            {
                r.InsertBytes(dataSet, 10);
                byte[] outBuffer = new byte[10];
                Assert.Equal(10, r.GetBuffer(outBuffer, 10));
                Assert.Equal(dataSet, outBuffer);
                r.ConsumeBytes(10);
            }

        }
    }
}

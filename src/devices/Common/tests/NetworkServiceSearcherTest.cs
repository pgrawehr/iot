// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Common;
using Xunit;

namespace Common.Tests
{
    public class NetworkServiceSearcherTest
    {
        [Fact]
        public void GetPrimaryNetworkInterface()
        {
            var result = NetworkServiceSearcher.GetPrimaryNetworkInterface();
            Assert.NotEqual(result.Address, IPAddress.Loopback);

            var list = NetworkServiceSearcher.GetAllValidAddressesInSubnet(result.Address, result.Mask);
            Assert.NotNull(list);
            Assert.True(list.Count > 16);
        }

        [Fact]
        public void GetAllValidAddressesInSubnet()
        {
            var list = NetworkServiceSearcher.GetAllValidAddressesInSubnet(IPAddress.Parse("192.168.1.10"),
                IPAddress.Parse("255.255.255.0"));
            Assert.NotNull(list);
            Assert.Equal(254, list.Count);
            Assert.Equal(IPAddress.Parse("192.168.1.1"), list[0]);
        }
    }
}

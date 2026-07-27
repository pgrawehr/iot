// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        private async Task<bool> IsYachtDevicesInterface(HttpClient client, IPAddress candidate, string expectedIdentifier)
        {
            try
            {
                using CancellationTokenSource ts = new CancellationTokenSource(500);
                var uri = new Uri($"http://{candidate.ToString()}/", UriKind.Absolute);
                var reply = await client.GetAsync(uri, ts.Token);
                // The header contains a single entry with the declaration "YDWG", which should
                // be enough to identify the device
                if (reply.IsSuccessStatusCode && reply.Headers.Any(x => x.Value.Any(y => y.Equals(expectedIdentifier, StringComparison.OrdinalIgnoreCase))))
                {
                    return true;
                }
            }
            catch (Exception x) when (x is UnauthorizedAccessException or SocketException or OperationCanceledException)
            {
                return false;
            }

            return false;
        }

        [Fact]
        public async Task FindDevice()
        {
            var interf = NetworkServiceSearcher.GetPrimaryNetworkInterface();
            var list = NetworkServiceSearcher.GetAllValidAddressesInSubnet(interf.Address, interf.Mask);
            using (var client = new HttpClient())
            {
                foreach (var candidate in list)
                {
                    if (await IsYachtDevicesInterface(client, candidate, "YDWG"))
                    {
                        return;
                    }
                }
            }

            Assert.Fail("No device found");
        }
    }
}

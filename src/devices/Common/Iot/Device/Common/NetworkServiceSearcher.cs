// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    public class NetworkServiceSearcher
    {
        /// <summary>
        /// Searches the subnet we're currently in for a service on the given port, using a test function to
        /// obtain data.
        /// </summary>
        /// <param name="expectedPort">The port where the service is expected to be</param>
        /// <param name="tester">A function to check whether it's the service we want. This will only
        /// be called if the port is open</param>
        /// <returns>The IP Address of the first server offering the expected service</returns>
        public static IPAddress SearchSubnetForService(int expectedPort, Func<TcpClient, bool> tester)
        {
            return IPAddress.Loopback;
        }

        /// <summary>
        /// Get the default IP address to bind to
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetLocalIPv4Address()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }

            return IPAddress.Loopback;
        }

        public static List<IPAddress> GetAllValidAddressesInSubnet(IPAddress addressInSubnet, IPAddress subnetMask)
        {
            byte[] ipAddressBytes = addressInSubnet.GetAddressBytes().Reverse().ToArray();
            if (ipAddressBytes.Length != 4)
            {
                throw new NotSupportedException("This method only supports IPv4 addresses");
            }

            byte[] subnetMaskBytes = subnetMask.GetAddressBytes().Reverse().ToArray();
            if (subnetMaskBytes.Length != 4)
            {
                throw new NotSupportedException("The subnet mask must be IPv4");
            }

            UInt32 rawaddress = BitConverter.ToUInt32(ipAddressBytes);
            UInt32 rawsubnetMask = BitConverter.ToUInt32(subnetMaskBytes);
            UInt32 rawsubnetMaskInversed = rawsubnetMask ^ 0xFFFFFFFF;
            // Now this is the starting address (not inclusive, since address x.x.x.0 is not available)
            rawaddress = rawaddress & rawsubnetMask;
            List<IPAddress> reply = new List<IPAddress>();
            UInt32 offset = 1;
            while (true)
            {
                UInt32 thisAddress = rawaddress + offset;
                byte[] addrBytes = BitConverter.GetBytes(thisAddress).Reverse().ToArray();
                IPAddress addressToUse = new IPAddress(addrBytes);
                if (!addressToUse.Equals(addressInSubnet))
                {
                    // Don't add our own
                    reply.Add(addressToUse);
                }

                offset++;
                // Abort when our offset is equal to the subnet mask (since address x.x.x.255 is also not available)
                if (offset >= rawsubnetMaskInversed)
                {
                    break;
                }
            }

            return reply;
        }

        public static (IPAddress Address, IPAddress Mask) GetPrimaryNetworkInterface()
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in adapters)
            {
                if (adapter.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                var prop = adapter.GetIPProperties();
                foreach (var uni in prop.UnicastAddresses)
                {
                    if (uni.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return (uni.Address, uni.IPv4Mask);
                    }
                }
            }

            return (IPAddress.Loopback, IPAddress.Parse("255.255.255.0"));
        }
    }
}

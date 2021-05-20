// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using Iot.Device.Arduino;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using Iot.Device.Ssd13xx;
using Iot.Device.Ssd13xx.Commands;
using Iot.Device.Ssd13xx.Samples;
using Ssd1306Cmnds = Iot.Device.Ssd13xx.Commands.Ssd1306Commands;
using Ssd1327Cmnds = Iot.Device.Ssd13xx.Commands.Ssd1327Commands;

Console.WriteLine("Hello Ssd1306 Sample!");

#if SSD1327
using Ssd1327 device = GetSsd1327WithI2c();
InitializeSsd1327(device);
ClearScreenSsd1327(device);
//SendMessage(device, "Hello .NET IoT!");
SendMessage(device, DisplayIpAddress());
#else
using Ssd1306 device = GetSsd1306WithI2c();
device.Initialize();
device.ClearScreen();
// SendMessage(device, "Hello .NET IoT!!!");
// SendMessage(device, DisplayIpAddress());
DisplayImages(device);
DisplayClock(device);
device.ClearScreen();
#endif

I2cDevice GetI2CDevice()
{
    Console.WriteLine("Using I2C protocol");

    I2cConnectionSettings connectionSettings = new(1, 0x3C);
    return I2cDevice.Create(connectionSettings);
}

Ssd1327 GetSsd1327WithI2c()
{
    return new Ssd1327(GetI2CDevice());
}

Ssd1306 GetSsd1306WithI2c()
{
    return new Ssd1306(GetI2CDevice());
}

string DisplayIpAddress()
{
    string? ipAddress = GetIpAddress();

    if (ipAddress is null)
    {
        return $"IP:{ipAddress}";
    }
    else
    {
        return $"Error: IP Address Not Found";
    }
}

void DisplayImages(Ssd1306 ssd1306)
{
    Console.WriteLine("Display Images");
    foreach (var image_name in Directory.GetFiles("images", "*.bmp").OrderBy(f => f))
    {
        using Image<L16> image = Image.Load<L16>(image_name);
        ssd1306.DisplayImage(image);
        Thread.Sleep(5000);
    }
}

void DisplayClock(Ssd1306 ssd1306)
{
    Console.WriteLine("Display clock");
    var fontSize = 25;
    var font = "DejaVu Sans";
    var fontsys = SystemFonts.CreateFont(font, fontSize, FontStyle.Italic);
    var y = 0;

    foreach (var i in Enumerable.Range(0, 100))
    {
        using (Image<Rgba32> image = new Image<Rgba32>(128, 32))
        {
            if (image.TryGetSinglePixelSpan(out Span<Rgba32> imageSpan))
            {
                imageSpan.Fill(Color.Black);
            }

            image.Mutate(ctx => ctx
                .DrawText(DateTime.Now.ToString("HH:mm:ss"), fontsys, Color.White,
                    new SixLabors.ImageSharp.PointF(0, y)));

            using (Image<L16> image_t = image.CloneAs<L16>())
            {
                ssd1306.DisplayImage(image_t);
            }

            y++;
            if (y >= image.Height)
            {
                y = 0;
            }

            Thread.Sleep(100);
        }
    }
}

// Referencing https://stackoverflow.com/questions/6803073/get-local-ip-address
string? GetIpAddress()
{
    // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection).
    NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

    foreach (NetworkInterface network in networkInterfaces)
    {
        // Read the IP configuration for each network
        IPInterfaceProperties properties = network.GetIPProperties();

        if (network.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
            network.OperationalStatus == OperationalStatus.Up &&
            !network.Description.ToLower().Contains("virtual") &&
            !network.Description.ToLower().Contains("pseudo"))
        {
            // Each network interface may have multiple IP addresses.
            foreach (IPAddressInformation address in properties.UnicastAddresses)
            {
                // We're only interested in IPv4 addresses for now.
                if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                // Ignore loopback addresses (e.g., 127.0.0.1).
                if (IPAddress.IsLoopback(address.Address))
                {
                    continue;
                }

                return address.Address.ToString();
            }
        }
    }

    return null;
}

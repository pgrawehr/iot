using System;
using System.Linq;
using System.Collections.Generic;
using Iot.Device.Ssd13xx.Commands;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Iot.Device.Ssd13xx
{
    /// <summary>
    /// Extension methods for Ssd1306 class.
    /// </summary>
    public static class Ssd13xxExtensions
    {
        // Port from https://github.com/adafruit/Adafruit_Python_SSD1306/blob/8819e2d203df49f2843059d981b7347d9881c82b/Adafruit_SSD1306/SSD1306.py#L184

        /// <summary>
        /// Extension method to display image using Ssd1306 device.
        /// </summary>
        /// <param name="s">Ssd1306 object.</param>
        /// <param name="image">Image to display.</param>
        public static void DisplayImage(this Ssd1306 s, Image<L16> image)
        {
            Int16 width = 128;
            Int16 pages = 4;
            List<byte> buffer = new();

            for (int page = 0; page < pages; page++)
            {
                for (int x = 0; x < width; x++)
                {
                    int bits = 0;
                    for (byte bit = 0; bit < 8; bit++)
                    {
                        bits = bits << 1;
                        bits |= image[x, page * 8 + 7 - bit].PackedValue > 0 ? 1 : 0;
                    }

                    buffer.Add((byte)bits);
                }
            }

            int chunk_size = 16;
            for (int i = 0; i < buffer.Count; i += chunk_size)
            {
                s.SendData(buffer.Skip(i).Take(chunk_size).ToArray());
            }
        }

        /// <summary>
        /// Initializes the display for 128x32 pixels
        /// </summary>
        public static void Initialize(this Ssd1306 device)
        {
            device.SendCommand(new SetDisplayOff());
            device.SendCommand(new Commands.Ssd1306Commands.SetDisplayClockDivideRatioOscillatorFrequency(0x00, 0x08));
            device.SendCommand(new SetMultiplexRatio(0x1F));
            device.SendCommand(new Commands.Ssd1306Commands.SetDisplayOffset(0x00));
            device.SendCommand(new Commands.Ssd1306Commands.SetDisplayStartLine(0x00));
            device.SendCommand(new Commands.Ssd1306Commands.SetChargePump(true));
            device.SendCommand(
                new Commands.Ssd1306Commands.SetMemoryAddressingMode(Commands.Ssd1306Commands.SetMemoryAddressingMode.AddressingMode
                    .Horizontal));
            device.SendCommand(new Commands.Ssd1306Commands.SetSegmentReMap(true));
            device.SendCommand(new Commands.Ssd1306Commands.SetComOutputScanDirection(false));
            device.SendCommand(new Commands.Ssd1306Commands.SetComPinsHardwareConfiguration(false, false));
            device.SendCommand(new SetContrastControlForBank0(0x8F));
            device.SendCommand(new Commands.Ssd1306Commands.SetPreChargePeriod(0x01, 0x0F));
            device.SendCommand(
                new Commands.Ssd1306Commands.SetVcomhDeselectLevel(Commands.Ssd1306Commands.SetVcomhDeselectLevel.DeselectLevel.Vcc1_00));
            device.SendCommand(new Commands.Ssd1306Commands.EntireDisplayOn(false));
            device.SendCommand(new Commands.Ssd1306Commands.SetNormalDisplay());
            device.SendCommand(new SetDisplayOn());
            device.SendCommand(new Commands.Ssd1306Commands.SetColumnAddress());
            device.SendCommand(new Commands.Ssd1306Commands.SetPageAddress(Commands.Ssd1306Commands.PageAddress.Page1,
                Commands.Ssd1306Commands.PageAddress.Page3));
        }

        /// <summary>
        /// Initializes the display for 96x96
        /// </summary>
        public static void Initialize(this Ssd1327 device)
        {
            device.SendCommand(new Commands.Ssd1327Commands.SetUnlockDriver(true));
            device.SendCommand(new SetDisplayOff());
            device.SendCommand(new SetMultiplexRatio(0x5F));
            device.SendCommand(new Commands.Ssd1327Commands.SetDisplayStartLine());
            device.SendCommand(new Commands.Ssd1327Commands.SetDisplayOffset(0x5F));
            device.SendCommand(new Commands.Ssd1327Commands.SetReMap());
            device.SendCommand(new Commands.Ssd1327Commands.SetInternalVddRegulator(true));
            device.SendCommand(new SetContrastControlForBank0(0x53));
            device.SendCommand(new Commands.Ssd1327Commands.SetPhaseLength(0X51));
            device.SendCommand(new Commands.Ssd1327Commands.SetDisplayClockDivideRatioOscillatorFrequency(0x01, 0x00));
            device.SendCommand(new Commands.Ssd1327Commands.SelectDefaultLinearGrayScaleTable());
            device.SendCommand(new Commands.Ssd1327Commands.SetPreChargeVoltage(0x08));
            device.SendCommand(new Commands.Ssd1327Commands.SetComDeselectVoltageLevel(0X07));
            device.SendCommand(new Commands.Ssd1327Commands.SetSecondPreChargePeriod(0x01));
            device.SendCommand(new Commands.Ssd1327Commands.SetSecondPreChargeVsl(true));
            device.SendCommand(new Commands.Ssd1327Commands.SetNormalDisplay());
            device.SendCommand(new DeactivateScroll());
            device.SendCommand(new SetDisplayOn());
            device.SendCommand(new Commands.Ssd1327Commands.SetRowAddress());
            device.SendCommand(new Commands.Ssd1327Commands.SetColumnAddress());
        }

        /// <summary>
        /// Clears the display
        /// </summary>
        public static void ClearScreen(this Ssd1306 device)
        {
            device.SendCommand(new Commands.Ssd1306Commands.SetColumnAddress());
            device.SendCommand(new Commands.Ssd1306Commands.SetPageAddress(Commands.Ssd1306Commands.PageAddress.Page0,
                Commands.Ssd1306Commands.PageAddress.Page3));

            for (int cnt = 0; cnt < 32; cnt++)
            {
                byte[] data = new byte[16];
                device.SendData(data);
            }
        }

        /// <summary>
        /// Clears the display
        /// </summary>
        /// <param name="device">The device</param>
        public static void ClearScreen(this Ssd1327 device)
        {
            device.ClearDisplay();
        }

        /// <summary>
        /// Writes text to the display
        /// </summary>
        /// <param name="device">The display</param>
        /// <param name="message">The text</param>
        public static void SendMessage(this Ssd1306 device, string message)
        {
            device.SendCommand(new Commands.Ssd1306Commands.SetColumnAddress());
            device.SendCommand(new Commands.Ssd1306Commands.SetPageAddress(Commands.Ssd1306Commands.PageAddress.Page0,
                Commands.Ssd1306Commands.PageAddress.Page3));

            foreach (char character in message)
            {
                device.SendData(BasicFont.GetCharacterBytes(character));
            }
        }

        /// <summary>
        /// Writes text to the display
        /// </summary>
        /// <param name="device">The display</param>
        /// <param name="message">The text</param>
        public static void SendMessageSsd1327(this Ssd1327 device, string message)
        {
            device.SetRowAddress(0x00, 0x07);

            foreach (char character in message)
            {
                byte[] charBitMap = BasicFont.GetCharacterBytes(character);
                List<byte> data = new List<byte>();
                for (var i = 0; i < charBitMap.Length; i = i + 2)
                {
                    for (var j = 0; j < 8; j++)
                    {
                        byte cdata = 0x00;
                        int bit1 = (byte)((charBitMap[i] >> j) & 0x01);
                        cdata |= (bit1 == 1) ? (byte)0xF0 : (byte)0x00;
                        var secondBitIndex = i + 1;
                        if (secondBitIndex < charBitMap.Length)
                        {
                            int bit2 = (byte)((charBitMap[i + 1] >> j) & 0x01);
                            cdata |= (bit2 == 1) ? (byte)0x0F : (byte)0x00;
                        }

                        data.Add(cdata);
                    }
                }

                device.SendData(data.ToArray());
            }
        }

    }
}

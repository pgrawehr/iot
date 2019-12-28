using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Globalization;
using System.Text;
using Iot.Device.CharacterLcd;

namespace DisplayControl
{
    public class DataContainer
    {
        I2cDevice m_displayDevice = null;
        ICharacterLcd m_characterLcd = null;
        LcdConsole m_lcdConsole = null;

        List<SensorValueSource> m_sensorValueSources;

        public DataContainer(GpioController controller)
        {
            Controller = controller;
            m_sensorValueSources = new List<SensorValueSource>();
        }

        public GpioController Controller { get; }

        public void InitializeDisplay()
        {
            m_displayDevice = I2cDevice.Create(new I2cConnectionSettings(1, 0x27)))
            var lcdInterface = LcdInterface.CreateI2c(m_displayDevice, false);
            m_characterLcd = new Lcd2004(lcdInterface);

            m_characterLcd.UnderlineCursorVisible = false;
            m_characterLcd.BacklightOn = true;
            m_characterLcd.DisplayOn = true;
            m_characterLcd.Clear();
            m_characterLcd.Write("== Startup ==");
            m_lcdConsole = new LcdConsole(m_characterLcd, "A02", false);
            LoadEncoding();
            m_lcdConsole.Clear();
            m_lcdConsole.Write("== Ready ==");
        }

        public void InitializeSensors()
        {
            
        }

        public void Initialize()
        {
            InitializeDisplay();
            InitializeSensors();
        }

        /// <summary>
        /// Loads the de-CH encoding to the display (even after it has been overriden)
        /// </summary>
        public void LoadEncoding()
        {
            m_lcdConsole.LoadEncoding(LcdConsole.CreateEncoding(CultureInfo.CreateSpecificCulture("de-CH"), "A02"));
        }

        public void ShutDown()
        {
            m_lcdConsole.Dispose();
            m_lcdConsole = null;
            m_characterLcd.Dispose();
            m_characterLcd = null;
            m_displayDevice.Dispose();
            m_displayDevice = null;
        }
    }
}

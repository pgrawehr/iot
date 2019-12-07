// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Iot.Device.CharacterLcd
{
    /// <summary>
    /// Displays a value and an unit in a big font on an LCD Display.
    /// Requires a display with at least 20x4 characters
    /// </summary>
    public class LcdValueUnitDisplay
    {
        private readonly ICharacterLcd _lcd;
        private LcdCharacterEncoding _encoding;
        private CultureInfo _culture;
        private char _currentSeparationChar;

        /// <summary>
        /// Creates an instance of <see cref="LcdValueUnitDisplay"/>
        /// </summary>
        /// <param name="lcd">Interface to the display</param>
        /// <param name="culture">User culture</param>
        public LcdValueUnitDisplay(ICharacterLcd lcd, CultureInfo culture)
        {
            _lcd = lcd;
            _culture = culture;
            if (lcd.Size.Width < 20 || lcd.Size.Height < 4)
            {
                throw new NotSupportedException("This class can only run on displays with at least 20x4 characters.");
            }
            if (lcd.NumberOfCustomCharactersSupported < 8)
            {
                throw new NotSupportedException("This class can only run on displays with 8 or more custom character slots");
            }
        }

        /// <summary>
        /// Initializes the display for use as a big-number display.
        /// Configures the display with some graphic blocks for the display of big numbers. 
        /// </summary>
        /// <param name="romName">Name of the character Rom, required to properly print culture-specific characters in the small text display</param>
        /// <param name="factory">Encoding factory or null</param>
        public void InitForRom(string romName, LcdCharacterEncodingFactory factory = null)
        {
            if (factory == null)
            {
                factory = new LcdCharacterEncodingFactory();
            }
            // Create the default encoding for the current ROM and culture, but leave the custom characters away. 
            var encoding = factory.Create(_culture, romName, ' ', 0);
            Dictionary<byte, byte[]> specialGraphicsRequired = new Dictionary<byte, byte[]>();
            CreateSpecialChars(specialGraphicsRequired);
            for (byte i = 0; i < specialGraphicsRequired.Count; i++)
            {
                _lcd.CreateCustomCharacter(i, specialGraphicsRequired[i]);
            }
            _currentSeparationChar = ' '; // To make sure the next function doesn't just skip the initialization
            LoadSeparationChar(_currentSeparationChar);
             _encoding = encoding;
            _lcd.BlinkingCursorVisible = false;
            _lcd.UnderlineCursorVisible = false;
            _lcd.BacklightOn = true;
            _lcd.DisplayOn = true;
            _lcd.Clear();
        }

        /// <summary>
        /// Display the current time
        /// </summary>
        /// <param name="dateTime">Time to display</param>
        /// <param name="format">Time format specifier, default "t" (default short time format with hours and minutes and eventually AM/PM).
        /// Anything after the first space in the formatted string is printed as small text. This will for instance be AM/PM when the format specifier "T" is used, 
        /// since only 6 chars (and two separators) fit on the display.</param>
        public void DisplayTime(DateTime dateTime, string format = "t")
        {
            string toDisplay = dateTime.ToString(format, _culture);
            string smallText = string.Empty;
            int spaceIdx = toDisplay.IndexOf(' ');
            if (spaceIdx > 0)
            {
                smallText = toDisplay.Substring(spaceIdx + 1);
                toDisplay = toDisplay.Substring(0, spaceIdx);
            }
            StringBuilder[] lines = CreateLinesFromText(toDisplay, smallText, 0);
            UpdateDisplay(lines);
        }

        /// <summary>
        /// Display the given value/unit pair. The value must be pre-formatted with the required number of digits, ie. "2.01". 
        /// The value should only contain one of ".", ":" or ",", or the printed result may be unexpected. 
        /// </summary>
        /// <param name="formattedValue">Pre-formatted value to print</param>
        /// <param name="unitText">Unit or name of value. This is printed in normal small font on the bottom right corner of the display. </param>
        public void DisplayValue(string formattedValue, string unitText = "")
        {
            var lines = CreateLinesFromText(formattedValue, unitText, 0);
            UpdateDisplay(lines);
        }

        public Task DisplayBigTextAsync(string text, TimeSpan scrollSpeed, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {

            });
        }

        private StringBuilder[] CreateLinesFromText(string bigText, string smallText, int startPosition)
        {
            int xPosition = startPosition; // The current x position during drawing. We draw 4-chars high letters, but with variable width

            void Insert(StringBuilder[] builder, int row, params byte[] ci)
            {
                for (int i = 0; i < ci.Length; i++)
                {
                    // Otherwise, there's no room for this character column
                    int column = xPosition + i;
                    if (column < _lcd.Size.Width && column >= 0)
                    {
                        builder[row][column] = (char)ci[i];
                    }
                }
            }
            StringBuilder[] ret = new StringBuilder[_lcd.Size.Height];
            for (int i = 0; i < _lcd.Size.Height; i++)
            {
                ret[i] = new StringBuilder();
                ret[i].Append(new string(' ', _lcd.Size.Width));
            }

            foreach(var c in bigText)
            {
                // Creates the big numbers. (char)32 is the empty field, for the others see CreateSpecialChars
                switch (c)
                {
                    case '1':
                        Insert(ret, 0, 32, 0, 6);
                        Insert(ret, 1, 0, 3, 6);
                        Insert(ret, 2, 32, 32, 6);
                        Insert(ret, 3, 32, 32, 6);
                        xPosition += 3;
                        break;
                    case '2':
                        Insert(ret, 0, 0, 6, 2);
                        Insert(ret, 1, 32, 0, 3);
                        Insert(ret, 2, 0, 3, 32);
                        Insert(ret, 3, 6, 6, 3);
                        xPosition += 3;
                        break;
                    case '3':
                        Insert(ret, 0, 0, 6, 2);
                        Insert(ret, 1, 32, 0, 3);
                        Insert(ret, 2, 32, 1, 2);
                        Insert(ret, 3, 1, 6, 3);
                        xPosition += 3;
                        break;
                    case '4':
                        Insert(ret, 0, 32, 0, 6);
                        Insert(ret, 1, 0, 3, 6);
                        Insert(ret, 2, 5, 5, 6);
                        Insert(ret, 3, 32, 32, 6);
                        xPosition += 3;
                        break;
                    case '5':
                        Insert(ret, 0, 6, 6, 6);
                        Insert(ret, 1, 6, 32, 32);
                        Insert(ret, 2, 5, 5, 2);
                        Insert(ret, 3, 1, 6, 3);
                        xPosition += 3;
                        break;
                    case '6':
                        Insert(ret, 0, 0, 6, 2);
                        Insert(ret, 1, 6, 32, 32);
                        Insert(ret, 2, 6, 5, 2);
                        Insert(ret, 3, 1, 6, 3);
                        xPosition += 3;
                        break;
                    case '7':
                        Insert(ret, 0, 6, 6, 6);
                        Insert(ret, 1, 32, 0, 3);
                        Insert(ret, 2, 0, 3, 32);
                        Insert(ret, 3, 3, 32, 32);
                        xPosition += 3;
                        break;
                    case '8':
                        Insert(ret, 0, 0, 6, 2);
                        Insert(ret, 1, 1, 4, 3);
                        Insert(ret, 2, 0, 5, 2);
                        Insert(ret, 3, 1, 6, 3);
                        xPosition += 3;
                        break;
                    case '9':
                        Insert(ret, 0, 0, 6, 2);
                        Insert(ret, 1, 6, 32, 6);
                        Insert(ret, 2, 5, 5, 6);
                        Insert(ret, 3, 1, 6, 3);
                        xPosition += 3;
                        break;
                    case '0':
                        Insert(ret, 0, 0, 6, 2);
                        Insert(ret, 1, 6, 0, 6);
                        Insert(ret, 2, 6, 3, 6);
                        Insert(ret, 3, 1, 6, 3);
                        xPosition += 3;
                        break;
                    case ':':
                        Insert(ret, 0, 32);
                        Insert(ret, 1, 7);
                        Insert(ret, 2, 7);
                        Insert(ret, 3, 32);
                        LoadSeparationChar(':');
                        xPosition += 1;
                        break;
                    case '.':
                        Insert(ret, 0, 32);
                        Insert(ret, 1, 32);
                        Insert(ret, 2, 32);
                        Insert(ret, 3, 7);
                        LoadSeparationChar('.');
                        xPosition += 1;
                        break;
                    case ',':
                        Insert(ret, 0, 32);
                        Insert(ret, 1, 32);
                        Insert(ret, 2, 32);
                        Insert(ret, 3, 7);
                        LoadSeparationChar(',');
                        xPosition += 1;
                        break;
                    case '-':
                        Insert(ret, 0, 32);
                        Insert(ret, 1, 32);
                        Insert(ret, 2, 5);
                        Insert(ret, 3, 32);
                        LoadSeparationChar(',');
                        xPosition += 1;
                        break;
                    case 'A':
                        Insert(ret, 0, 0, 6, 2);
                        Insert(ret, 1, 6, 32, 6);
                        Insert(ret, 2, 6, 5, 6);
                        Insert(ret, 3, 6, 32, 6);
                        xPosition += 3;
                        break;
                    case 'B':
                        Insert(ret, 0, 6, 6, 2);
                        Insert(ret, 1, 6, 32, 3);
                        Insert(ret, 2, 6, 5, 2);
                        Insert(ret, 3, 6, 6, 3);
                        xPosition += 3;
                        break;
                    case 'C':
                        Insert(ret, 0, 6, 6, 2);
                        Insert(ret, 1, 6, 32, 32);
                        Insert(ret, 2, 6, 32, 32);
                        Insert(ret, 3, 6, 6, 3);
                        xPosition += 3;
                        break;
                    case 'D':
                        Insert(ret, 0, 6, 6, 2);
                        Insert(ret, 1, 6, 32, 6);
                        Insert(ret, 2, 6, 32, 6);
                        Insert(ret, 3, 6, 6, 3);
                        xPosition += 3;
                        break;
                    case 'E':
                        Insert(ret, 0, 6, 6, 6);
                        Insert(ret, 1, 6, 4, 32);
                        Insert(ret, 2, 6, 5, 32);
                        Insert(ret, 3, 6, 6, 6);
                        xPosition += 3;
                        break;
                    case 'F':
                        Insert(ret, 0, 6, 6, 6);
                        Insert(ret, 1, 6, 4, 32);
                        Insert(ret, 2, 6, 5, 32);
                        Insert(ret, 3, 6, 32, 32);
                        xPosition += 3;
                        break;
                    case 'G':
                        Insert(ret, 0, 0, 6, 2);
                        Insert(ret, 1, 6, 32, 5);
                        Insert(ret, 2, 6, 5, 2);
                        Insert(ret, 3, 1, 6, 3);
                        xPosition += 3;
                        break;
                    case 'H':
                        Insert(ret, 0, 6, 32, 6);
                        Insert(ret, 1, 6, 4, 6);
                        Insert(ret, 2, 6, 5, 6);
                        Insert(ret, 3, 6, 32, 6);
                        xPosition += 3;
                        break;
                    case 'I':
                        Insert(ret, 0, 6, 6, 6);
                        Insert(ret, 1, 32, 6, 32);
                        Insert(ret, 2, 32, 6, 32);
                        Insert(ret, 3, 6, 6, 6);
                        xPosition += 3;
                        break;
                    case 'J':
                        Insert(ret, 0, 32, 32, 6);
                        Insert(ret, 1, 32, 32, 6);
                        Insert(ret, 2, 32, 32, 6);
                        Insert(ret, 3, 1, 6, 3);
                        xPosition += 3;
                        break;
                    case 'K':
                        Insert(ret, 0, 6, 32, 0);
                        Insert(ret, 1, 6, 0, 3);
                        Insert(ret, 2, 6, 1, 2);
                        Insert(ret, 3, 6, 32, 1);
                        xPosition += 3;
                        break;
                    case 'L':
                        Insert(ret, 0, 6, 32, 32);
                        Insert(ret, 1, 6, 32, 32);
                        Insert(ret, 2, 6, 32, 32);
                        Insert(ret, 3, 6, 6, 3);
                        xPosition += 3;
                        break;
                    case 'M':
                        Insert(ret, 0, 6, 2, 0, 6);
                        Insert(ret, 1, 6, 1, 3, 6);
                        Insert(ret, 2, 6, 32, 32, 6);
                        Insert(ret, 3, 6, 32, 32, 6);
                        xPosition += 4;
                        break;
                    case 'N':
                        Insert(ret, 0, 6, 2, 6);
                        Insert(ret, 1, 6, 1, 6);
                        Insert(ret, 2, 6, 32, 6);
                        Insert(ret, 3, 6, 32, 6);
                        xPosition += 3;
                        break;
                    case 'O':
                        Insert(ret, 0, 0, 6, 2);
                        Insert(ret, 1, 6, 32, 6);
                        Insert(ret, 2, 6, 32, 6);
                        Insert(ret, 3, 1, 6, 3);
                        xPosition += 3;
                        break;
                    case 'P':
                        Insert(ret, 0, 6, 6, 2);
                        Insert(ret, 1, 6, 32, 6);
                        Insert(ret, 2, 6, 5, 5);
                        Insert(ret, 3, 6, 32, 32);
                        xPosition += 3;
                        break;
                    case 'Q':
                        Insert(ret, 0, 0, 6, 2);
                        Insert(ret, 1, 6, 32, 6);
                        Insert(ret, 2, 6, 32, 6);
                        Insert(ret, 3, 1, 6, 6);
                        xPosition += 3;
                        break;
                    case 'R':
                        Insert(ret, 0, 6, 6, 2);
                        Insert(ret, 1, 6, 32, 6);
                        Insert(ret, 2, 6, 6, 3);
                        Insert(ret, 3, 6, 1, 2);
                        xPosition += 3;
                        break;
                    case 'S':
                        Insert(ret, 0, 0, 6, 2);
                        Insert(ret, 1, 1, 2, 32);
                        Insert(ret, 2, 32, 1, 2);
                        Insert(ret, 3, 1, 6, 3);
                        xPosition += 3;
                        break;
                    case 'T':
                        Insert(ret, 0, 6, 6, 6);
                        Insert(ret, 1, 32, 6, 32);
                        Insert(ret, 2, 32, 6, 32);
                        Insert(ret, 3, 32, 6, 32);
                        xPosition += 3;
                        break;
                    case 'U':
                        Insert(ret, 0, 6, 32, 6);
                        Insert(ret, 1, 6, 32, 6);
                        Insert(ret, 2, 6, 32, 6);
                        Insert(ret, 3, 1, 6, 3);
                        xPosition += 3;
                        break;
                    case 'V':
                        Insert(ret, 0, 6, 32, 32, 6);
                        Insert(ret, 1, 6, 32, 32, 6);
                        Insert(ret, 2, 1, 2, 0, 3);
                        Insert(ret, 3, 32, 1, 3, 32);
                        xPosition += 4;
                        break;
                    case 'W':
                        Insert(ret, 0, 6, 32, 32, 6);
                        Insert(ret, 1, 6, 32, 32, 6);
                        Insert(ret, 2, 6, 0, 2, 6);
                        Insert(ret, 3, 6, 3, 1, 6);
                        xPosition += 4;
                        break;
                    case 'X':
                        Insert(ret, 0, 1, 2, 0, 3);
                        Insert(ret, 1, 32, 1, 3, 32);
                        Insert(ret, 2, 32, 0, 2, 32);
                        Insert(ret, 3, 0, 3, 1, 2);
                        xPosition += 4;
                        break;
                    case 'Y':
                        Insert(ret, 0, 1, 2, 0, 3);
                        Insert(ret, 1, 32, 1, 6, 32);
                        Insert(ret, 2, 32, 32, 6, 32);
                        Insert(ret, 3, 32, 32, 6, 32);
                        xPosition += 4;
                        break;
                    case 'Z':
                        Insert(ret, 0, 6, 6, 6);
                        Insert(ret, 1, 32, 0, 3);
                        Insert(ret, 2, 0, 3, 32);
                        Insert(ret, 3, 6, 6, 6);
                        xPosition += 3;
                        break;
                    case 'Ω':
                        Insert(ret, 0, 0, 6, 6, 2);
                        Insert(ret, 1, 6, 32, 32, 6);
                        Insert(ret, 2, 6, 2, 0, 6);
                        Insert(ret, 3, 4, 3, 1, 4);
                        xPosition += 4;
                        break;
                    case 'a':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 4, 4, 32);
                        Insert(ret, 2, 0, 4, 2);
                        Insert(ret, 3, 1, 4, 3);
                        xPosition += 3;
                        break;
                    case 'b':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 6, 32, 32);
                        Insert(ret, 2, 6, 5, 2);
                        Insert(ret, 3, 6, 4, 3);
                        xPosition += 3;
                        break;
                    case 'c':
                        Insert(ret, 0, 32, 32);
                        Insert(ret, 1, 32, 32);
                        Insert(ret, 2, 0, 5);
                        Insert(ret, 3, 1, 4);
                        xPosition += 2;
                        break;
                    case 'd':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 32, 32, 6);
                        Insert(ret, 2, 0, 5, 6);
                        Insert(ret, 3, 1, 4, 6);
                        xPosition += 3;
                        break;
                    case 'e':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 4, 4, 4);
                        Insert(ret, 2, 6, 4, 2);
                        Insert(ret, 3, 1, 4, 3);
                        xPosition += 3;
                        break;
                    case 'f':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 32, 0, 5);
                        Insert(ret, 2, 5, 6, 5);
                        Insert(ret, 3, 32, 6, 32);
                        xPosition += 3;
                        break;
                    case 'g':
                        Insert(ret, 0, 32, 32);
                        Insert(ret, 1, 0, 5, 6);
                        Insert(ret, 2, 6, 4, 6);
                        Insert(ret, 3, 32, 4, 3);
                        xPosition += 3;
                        break;
                    case 'h':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 6, 32, 32);
                        Insert(ret, 2, 6, 6, 2);
                        Insert(ret, 3, 6, 32, 6);
                        xPosition += 3;
                        break;
                    case 'i':
                        Insert(ret, 0, 32, 32);
                        Insert(ret, 1, 5, 32);
                        Insert(ret, 2, 6, 5);
                        Insert(ret, 3, 6, 32);
                        xPosition += 2;
                        break;
                    case 'j':
                        Insert(ret, 0, 32, 32);
                        Insert(ret, 1, 5, 6);
                        Insert(ret, 2, 4, 6);
                        Insert(ret, 3, 4, 3);
                        xPosition += 2;
                        break;
                    case 'k':
                        Insert(ret, 0, 32, 32);
                        Insert(ret, 1, 6, 32);
                        Insert(ret, 2, 6, 0);
                        Insert(ret, 3, 6, 1);
                        xPosition += 2;
                        break;
                    case 'l':
                        Insert(ret, 0, 32, 32);
                        Insert(ret, 1, 6, 32);
                        Insert(ret, 2, 6, 32);
                        Insert(ret, 3, 1, 4);
                        xPosition += 2;
                        break;
                    case 'm':
                        Insert(ret, 0, 32, 32, 32, 32);
                        Insert(ret, 1, 32, 32, 32, 32);
                        Insert(ret, 2, 0, 2, 0, 2);
                        Insert(ret, 3, 6, 32, 32, 6);
                        xPosition += 4;
                        break;
                    case 'n':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 32, 32, 32);
                        Insert(ret, 2, 6, 6, 2);
                        Insert(ret, 3, 6, 32, 6);
                        xPosition += 3;
                        break;
                    case 'o':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 32, 32, 32);
                        Insert(ret, 2, 0, 5, 2);
                        Insert(ret, 3, 1, 4, 3);
                        xPosition += 3;
                        break;
                    case 'p':
                        Insert(ret, 0, 32, 32);
                        Insert(ret, 1, 6, 5, 2);
                        Insert(ret, 2, 6, 4, 6);
                        Insert(ret, 3, 6, 32, 32);
                        xPosition += 3;
                        break;
                    case 'q':
                        Insert(ret, 0, 32, 32);
                        Insert(ret, 1, 0, 5, 6);
                        Insert(ret, 2, 1, 4, 6);
                        Insert(ret, 3, 32, 32, 6);
                        xPosition += 3;
                        break;
                    case 'r':
                        Insert(ret, 0, 32, 32);
                        Insert(ret, 1, 32, 32);
                        Insert(ret, 2, 6, 0);
                        Insert(ret, 3, 6, 32);
                        xPosition += 2;
                        break;
                    case 's':
                        Insert(ret, 0, 32, 32);
                        Insert(ret, 1, 0, 5, 5);
                        Insert(ret, 2, 1, 4, 2);
                        Insert(ret, 3, 4, 4, 3);
                        xPosition += 3;
                        break;
                    case 't':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 32, 0, 32);
                        Insert(ret, 2, 5, 6, 5);
                        Insert(ret, 3, 32, 6, 4);
                        xPosition += 3;
                        break;
                    case 'u':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 32, 32, 32);
                        Insert(ret, 2, 6, 32, 6);
                        Insert(ret, 3, 1, 4, 6);
                        xPosition += 3;
                        break;
                    case 'v':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 32, 32, 32);
                        Insert(ret, 2, 1, 2, 2);
                        Insert(ret, 3, 32, 1, 32);
                        xPosition += 3;
                        break;
                    case 'w':
                        Insert(ret, 0, 32, 32, 32, 32);
                        Insert(ret, 1, 32, 32, 32, 32);
                        Insert(ret, 2, 6, 32, 32, 6);
                        Insert(ret, 3, 1, 3, 1, 3);
                        xPosition += 4;
                        break;
                    case 'x':
                        Insert(ret, 0, 32, 32);
                        Insert(ret, 1, 32, 32);
                        Insert(ret, 2, 2, 0);
                        Insert(ret, 3, 3, 1);
                        xPosition += 2;
                        break;
                    case 'y':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 6, 32, 6);
                        Insert(ret, 2, 1, 4, 6);
                        Insert(ret, 3, 32, 4, 6);
                        xPosition += 3;
                        break;
                    case 'z':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 4, 4, 4);
                        Insert(ret, 2, 32, 0, 3);
                        Insert(ret, 3, 0, 4, 4);
                        xPosition += 3;
                        break;
                    case 'μ':
                        Insert(ret, 0, 32, 32, 32);
                        Insert(ret, 1, 32, 32, 32);
                        Insert(ret, 2, 6, 32, 6);
                        Insert(ret, 3, 6, 4, 3);
                        xPosition += 3;
                        break;
                    case ' ':
                        xPosition += 1;
                        break;
                }
            }

            // Right allign the small text (i.e. an unit)
            // It will eventually overwrite the last row of the rightmost digits, but that presumably is still readable. 
            int unitPosition = _lcd.Size.Width - smallText.Length;
            xPosition = unitPosition;
            var encodedSmallText = _encoding.GetBytes(smallText);
            for (int i = 0; i < encodedSmallText.Length; i++)
            {
                ret[3][xPosition + i] = (char)encodedSmallText[i];
            }
            return ret;
        }

        private void UpdateDisplay(StringBuilder[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                _lcd.SetCursorPosition(0, i);
                // Will again do a character translation, but that shouldn't hurt, as all characters in the input strings should be printable now. 
                _lcd.Write(lines[i].ToString());
            }
        }

        private void CreateSpecialChars(Dictionary<byte, byte[]> graphicChars)
        {
            graphicChars.Add(0, new byte[]
            {
                0b00001,
                0b00011,
                0b00011,
                0b00111,
                0b00111,
                0b01111,
                0b01111,
                0b11111
            });
            graphicChars.Add(1, new byte[]
            {
                0b11111,
                0b01111,
                0b01111,
                0b00111,
                0b00111,
                0b00011,
                0b00011,
                0b00001,
            });
            graphicChars.Add(2, new byte[]
            {
                0b10000,
                0b11000,
                0b11000,
                0b11100,
                0b11100,
                0b11110,
                0b11110,
                0b11111,
            });
            graphicChars.Add(3, new byte[]
            {
                0b11111,
                0b11110,
                0b11110,
                0b11100,
                0b11100,
                0b11000,
                0b11000,
                0b10000,
            });
            graphicChars.Add(4, new byte[]
            {
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b11111,
                0b11111,
                0b11111,
                0b11111
            });
            graphicChars.Add(5, new byte[]
            {
                0b11111,
                0b11111,
                0b11111,
                0b11111,
                0b00000,
                0b00000,
                0b00000,
                0b00000,
            });
            graphicChars.Add(6, new byte[]
            {
                0b11111,
                0b11111,
                0b11111,
                0b11111,
                0b11111,
                0b11111,
                0b11111,
                0b11111,
            });
            
        }

        /// <summary>
        /// Character code 7 is always used for the separation char, which is one of ":", "." or ",". 
        /// </summary>
        /// <param name="separationChar"></param>
        private void LoadSeparationChar(char separationChar)
        {
            if (separationChar == _currentSeparationChar)
            {
                return;
            }
            switch (separationChar)
            {

                case ':':
                    _lcd.CreateCustomCharacter(7, new byte[]
                    {
                0b00000,
                0b00000,
                0b01110,
                0b01110,
                0b01110,
                0b00000,
                0b00000,
                0b00000,
                    });
                    break;
                case '.':
                    _lcd.CreateCustomCharacter(7, new byte[]
                    {
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b01110,
                0b01110,
                0b01110,
                    });
                    break;
                case ',':
                    _lcd.CreateCustomCharacter(7, new byte[]
                    {
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b01110,
                0b01110,
                0b01100,
                0b01000,
                    });
                    break;
                default:
                    throw new NotImplementedException("Unknown separation char: " + separationChar);
            }
            _currentSeparationChar = separationChar;
        }
    }
}

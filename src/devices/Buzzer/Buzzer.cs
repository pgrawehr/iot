// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Pwm;
using System.Device.Pwm.Drivers;
using System.Threading;
using UnitsNet;

namespace Iot.Device.Buzzer
{
    /// <summary>
    /// Simple buzzer binding. Supports buzzers with ground, vcc and signal pins as well as buzzers with only ground and vcc pins.
    /// </summary>
    public class Buzzer : IDisposable
    {
        private PwmChannel _pwmChannel;

        /// <summary>
        /// Constructs Buzzer instance
        /// </summary>
        /// <param name="pinNumber">Pin connected to buzzer</param>
        public Buzzer(int pinNumber)
            : this(new SoftwarePwmChannel(pinNumber))
        {
        }

        /// <summary>
        /// Create Buzzer class instance with output on specified pin with specified channel.
        /// </summary>
        /// <param name="chip">The GPIO pin number in case of a software PWM. The chip in case of a hardware PWM.</param>
        /// <param name="channel">The channel to use in case of a hardware PWM.</param>
        public Buzzer(int chip, int channel)
            : this(PwmChannel.Create(chip, channel))
        {
        }

        /// <summary>
        /// Create Buzzer class instance with output on specified pin with specified channel using passed PWM controller.
        /// </summary>
        /// <param name="pwmChannel">The PWM controller to use during work.</param>
        public Buzzer(PwmChannel pwmChannel)
        {
            _pwmChannel = pwmChannel;
        }

        /// <summary>
        /// Set new or overwrite previously set frequency and start playing the sound.
        /// </summary>
        /// <param name="frequency">Tone frequency in Hertz.</param>
        public void StartPlaying(Frequency frequency)
        {
            _pwmChannel.Frequency = (int)frequency.Hertz;
            _pwmChannel.Start();
        }

        /// <summary>
        /// Stop playing tone.
        /// </summary>
        public void StopPlaying()
        {
            _pwmChannel.Stop();
        }

        /// <summary>
        /// Play tone of specific frequency for specified duration.
        /// </summary>
        /// <param name="frequency">Tone frequency.</param>
        /// <param name="duration">Playing duration.</param>
        public void PlayTone(Frequency frequency, Duration duration)
        {
            StartPlaying(frequency);
            Thread.Sleep(duration.ToTimeSpan());
            StopPlaying();
        }

        /// <summary>
        /// Dispose Buzzer.
        /// </summary>
        public void Dispose()
        {
            _pwmChannel?.Dispose();
            _pwmChannel = null;
        }
    }
}

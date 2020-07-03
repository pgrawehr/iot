// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Iot.Device.Buzzer.Samples
{
    /// <summary>
    /// A buzzer test application
    /// </summary>
    public class Program
    {
        // Alphabet song: https://en.wikipedia.org/wiki/Alphabet_song#/media/File:Alphabet_song.png
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1025:Code should not contain multiple whitespace in a row", Justification = "Readability")]
        internal static IList<MelodyElement> AlphabetSong = new List<MelodyElement>()
        {
            new NoteElement(Note.C, Octave.Fourth, NoteDuration.Quarter),   // A
            new NoteElement(Note.C, Octave.Fourth, NoteDuration.Quarter),   // B
            new NoteElement(Note.G, Octave.Fourth, NoteDuration.Quarter),   // C
            new NoteElement(Note.G, Octave.Fourth, NoteDuration.Quarter),   // D

            new NoteElement(Note.A, Octave.Fourth, NoteDuration.Quarter),   // E
            new NoteElement(Note.A, Octave.Fourth, NoteDuration.Quarter),   // F
            new NoteElement(Note.G, Octave.Fourth, NoteDuration.Half),      // G

            new NoteElement(Note.F, Octave.Fourth, NoteDuration.Quarter),   // H
            new NoteElement(Note.F, Octave.Fourth, NoteDuration.Quarter),   // I
            new NoteElement(Note.E, Octave.Fourth, NoteDuration.Quarter),   // J
            new NoteElement(Note.E, Octave.Fourth, NoteDuration.Quarter),   // K

            new NoteElement(Note.D, Octave.Fourth, NoteDuration.Eighth),    // L
            new NoteElement(Note.D, Octave.Fourth, NoteDuration.Eighth),    // M
            new NoteElement(Note.D, Octave.Fourth, NoteDuration.Eighth),    // N
            new NoteElement(Note.D, Octave.Fourth, NoteDuration.Eighth),    // O
            new NoteElement(Note.C, Octave.Fourth, NoteDuration.Half),      // P

            new NoteElement(Note.G, Octave.Fourth, NoteDuration.Quarter),   // Q
            new NoteElement(Note.G, Octave.Fourth, NoteDuration.Quarter),   // R
            new NoteElement(Note.F, Octave.Fourth, NoteDuration.Half),      // S

            new NoteElement(Note.E, Octave.Fourth, NoteDuration.Quarter),   // T
            new NoteElement(Note.E, Octave.Fourth, NoteDuration.Quarter),   // U
            new NoteElement(Note.D, Octave.Fourth, NoteDuration.Half),      // V

            new NoteElement(Note.G, Octave.Fourth, NoteDuration.Eighth),    // Dou-
            new NoteElement(Note.G, Octave.Fourth, NoteDuration.Eighth),    // ble
            new NoteElement(Note.G, Octave.Fourth, NoteDuration.Quarter),   // U
            new NoteElement(Note.F, Octave.Fourth, NoteDuration.Half),      // X

            new NoteElement(Note.E, Octave.Fourth, NoteDuration.Quarter),   // Y
            new NoteElement(Note.E, Octave.Fourth, NoteDuration.Quarter),   // and
            new NoteElement(Note.D, Octave.Fourth, NoteDuration.Half)       // Z
        };

        /// <summary>
        /// Main entry point
        /// </summary>
        public static void Main(string[] args)
        {
            using (var player1 = new MelodyPlayer(new Buzzer(21)))
            using (var player2 = new MelodyPlayer(new Buzzer(26)))
            {
                Task.WaitAll(
                    Task.Run(() => player1.Play(AlphabetSong, 100, -12)),
                    Task.Run(() => player2.Play(AlphabetSong, 100)));
            }
        }
    }
}

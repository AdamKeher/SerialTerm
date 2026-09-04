using System;
using System.IO;

namespace TerminalConsole
{
    partial class Program
    {
        // Console.KeyAvailable, ReadKey, TreatControlCAsInput and Clear all need
        // a real console handle and throw once stdin or stdout is redirected.
        // With input redirected there is nothing to type with, so SerialTerm
        // keeps listening and writing - `SerialTerm -P COM3 > session.log` is a
        // useful thing to be able to do.
        private static bool _keyboardAvailable;

        private static void DetectConsole()
        {
            _keyboardAvailable = !Console.IsInputRedirected;
        }

        private static void EnableControlCPassthrough()
        {
            if (!_keyboardAvailable)
                return;

            try
            {
                Console.TreatControlCAsInput = true;
            }
            catch (IOException) { }
        }

        private static void RestoreControlC()
        {
            if (!_keyboardAvailable)
                return;

            try
            {
                Console.TreatControlCAsInput = false;
            }
            catch (IOException) { }
        }

        private static bool KeyAvailable()
        {
            if (!_keyboardAvailable)
                return false;

            try
            {
                return Console.KeyAvailable;
            }
            catch (InvalidOperationException)
            {
                // input went away underneath us, stop asking
                _keyboardAvailable = false;
                return false;
            }
        }

        private static void ClearScreen()
        {
            lock (_consoleLock)
            {
                try
                {
                    Console.Clear();
                }
                catch (IOException) { }
            }
        }

        // Device output is written to the raw stdout stream under _consoleLock.
        // Everything SerialTerm says itself has to take the same lock, or the
        // two tear into each other - a help table shredded mid row by incoming
        // bytes, or the hint saving a half written line and painting it back.
        // Monitor is reentrant, so Say/SayLine nest inside a SayBlock freely.
        private static void Say(string text)
        {
            lock (_consoleLock)
            {
                Console.Write(text);
            }
        }

        private static void SayLine(string text = "")
        {
            lock (_consoleLock)
            {
                Console.WriteLine(text);
            }
        }

        // holds the lock across a run of writes that must not be interleaved
        private static void SayBlock(Action body)
        {
            lock (_consoleLock)
            {
                body();
            }
        }
    }
}

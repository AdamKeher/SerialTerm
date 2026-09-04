using System;
using System.IO;

namespace TerminalConsole
{
    partial class Program
    {
        // offered by number so a rate can be picked with one keystroke, in the
        // order they actually get reached for
        private static readonly int[] CommonBauds =
            { 115200, 9600, 57600, 38400, 19200, 4800, 2400, 1200, 230400, 460800, 921600 };

        // Ctrl+A #. Chasing an unknown rate meant quitting and relaunching for
        // each guess.
        private static void ChangeBaudCommand()
        {
            SayBlock(() =>
            {
                SayLine($"\r\nCurrent baud rate: {_serialPort.BaudRate}");

                for (int index = 0; index < CommonBauds.Length; index++)
                    SayLine($"  {index + 1,2}. {CommonBauds[index]}");

                Say($"New baud rate (1-{CommonBauds.Length}, a number, or blank to cancel): ");
            });

            string entry = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(entry))
            {
                SayLine("Unchanged.");
                return;
            }

            if (!int.TryParse(entry, out int value) || value <= 0)
            {
                SayLine($"'{entry}' is not a number.");
                return;
            }

            // a small number is a pick from the list, anything larger is a rate
            int baud = value <= CommonBauds.Length ? CommonBauds[value - 1] : value;

            SetBaudRate(baud);
        }

        private static void SetBaudRate(int baud)
        {
            if (baud == _serialPort.BaudRate)
            {
                SayLine($"Already at {baud} baud.");
                return;
            }

            bool wasOpen = _serialPort.IsOpen;
            int previous = _serialPort.BaudRate;

            try
            {
                // the rate can only be changed on a closed port, so the
                // connection is dropped and remade around it
                if (wasOpen)
                    _serialPort.Close();

                _serialPort.BaudRate = baud;

                if (wasOpen)
                    _serialPort.Open();

                SayLine($"Baud rate now {baud}.");
            }
            catch (Exception e) when (e is ArgumentOutOfRangeException || e is IOException
                                   || e is UnauthorizedAccessException || e is InvalidOperationException)
            {
                SayLine($"Could not set {baud} baud: {e.Message}");

                // put it back, so the reconnect loop is not left trying to open
                // the port at a rate the driver refused
                try
                {
                    _serialPort.BaudRate = previous;

                    if (wasOpen && !_serialPort.IsOpen)
                        _serialPort.Open();
                }
                catch (Exception) { }
            }
        }
    }
}

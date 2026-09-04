using System;
using System.CommandLine.Invocation;
using System.IO.Ports;
using System.Threading;

namespace TerminalConsole
{
    partial class Program
    {
        // keys are polled often enough not to hold up typing, while reconnection
        // is retried at a more relaxed pace
        private const int PollInterval = 5;
        private const int ReconnectInterval = 500;

        static void RootCommmandHandler(InvocationContext context, CommandLineOptions options)
        {
            DetectConsole();

            // The SerialPort property setters range check and throw. The options
            // are validated during parsing, so reaching this catch means a
            // combination we did not anticipate - report it in one line rather
            // than letting a stack trace out.
            try
            {
                _serialPort = GetSerialPort(options);
            }
            catch (ArgumentException e)
            {
                SayLine($"Invalid serial port settings: {e.Message}");
                return;
            }

            if (_serialPort == null)
                return;

            _escapeKey = ParseEscapeKey(options.escapeKey);
            _legacyKeys = options.legacyKeys;
            _hintEnabled = !options.noHint;

            // open serial port
            SayLine($"Connecting to: {SerialPortToString()}");
            SayLine($"Press {EscapeKeyName()} ? for the list of terminal keys");

            // set when the user has already been told the port is not available,
            // so the retry loop does not repeat itself
            bool reported = false;

            try
            {
                _serialPort.Open();
            }
            catch (UnauthorizedAccessException)
            {
                SayLine(PortInUseMessage());
                reported = true;
            }
            catch (Exception e)
            {
                SayLine($"Failed to open {_serialPort.PortName}: {e.Message}");
                reported = true;
            }

            // outside the open, so a reset that fails cannot be reported as the
            // port having failed to open
            if (_serialPort.IsOpen && options.resetEsp32)
                ResetEsp32(100);

            // Ctrl+C belongs to the connected device rather than to SerialTerm, and
            // the console has to be asked to interpret the escape sequences a
            // device sends before a full screen application can draw with them
            EnableControlCPassthrough();
            EnableVirtualTerminal();

            try
            {
                TerminalLoop(options, reported);
            }
            finally
            {
                RestoreConsoleMode();
                RestoreControlC();
            }
        }

        // wait while receiving data and handle disconnection and control keys
        private static void TerminalLoop(CommandLineOptions options, bool reported)
        {
            bool paused = false;
            DateTime lastConnectAttempt = DateTime.MinValue;
            _continue = true;

            while (_continue)
            {
                // handle serial disconnection and reconnection
                if (!paused && !_serialPort.IsOpen
                    && (DateTime.UtcNow - lastConnectAttempt).TotalMilliseconds >= ReconnectInterval)
                {
                    lastConnectAttempt = DateTime.UtcNow;

                    try
                    {
                        _serialPort.Open();
                        if (reported) SayLine("Reconnected.");
                        reported = false;
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        if (!reported) SayLine("Disconnected.");
                        if (options.disconnectExit)
                            return;
                        reported = true;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // the port is there but another program holds it open, so
                        // keep waiting rather than failing - it is released again
                        // when that program exits
                        if (!reported) SayLine(PortInUseMessage());
                        reported = true;
                    }
                    catch (System.IO.IOException) { }
                }

                // control keys
                if (KeyAvailable())
                    paused = ProcessKeys(paused);
                else
                    Thread.Sleep(PollInterval);
            }
        }

        private static string PortInUseMessage()
        {
            return $"{_serialPort.PortName} is in use by another program. Waiting for it to be released ...";
        }
    }
}

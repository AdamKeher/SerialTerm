using System;
using System.Globalization;

namespace TerminalConsole
{
    partial class Program
    {
        private static readonly char DefaultEscapeKey = ControlKey('A');

        // Every key is forwarded to the connected device, so remote applications
        // keep ESC, the function keys, the arrows and Ctrl+C. SerialTerm's own
        // commands sit behind an escape (prefix) key: press the prefix, then a
        // command key. Pressing the prefix twice sends it on to the device.
        private static char _escapeKey = DefaultEscapeKey;
        private static bool _escapePending;
        private static bool _legacyKeys;

        private static bool ProcessKeys(bool paused)
        {
            var key = Console.ReadKey(true);

            if (_escapePending)
            {
                _escapePending = false;
                HideHint();
                return EscapeCommand(key, paused);
            }

            if (key.KeyChar != '\0' && key.KeyChar == _escapeKey)
            {
                _escapePending = true;
                ShowHint();
                return paused;
            }

            if (_legacyKeys && LegacyCommand(key, ref paused))
                return paused;

            SendToPort(EncodeKey(key));

            return paused;
        }

        private static bool EscapeCommand(ConsoleKeyInfo key, bool paused)
        {
            // the prefix twice over sends the prefix character itself to the device
            if (key.KeyChar == _escapeKey)
            {
                SendToPort(new byte[] { (byte)_escapeKey });
                return paused;
            }

            if (key.KeyChar >= '1' && key.KeyChar <= '9' && RunMacro(key.KeyChar))
                return paused;

            switch (char.ToLowerInvariant(key.KeyChar))
            {
                case '?':
                case 'h':
                    DisplayHelp();
                    break;

                case 'q':
                case 'x':
                    _continue = false;
                    break;

                case 'c':
                    ClearScreen();
                    break;

                case 'd':
                    paused = TogglePause(paused);
                    break;

                case 'i':
                    SayLine($"\r\nConnected to: {SerialPortToString()}");
                    break;

                case 'e':
                    ResetEsp32Command();
                    break;

                case 'p':
                    paused = PicoProgrammingCommand();
                    break;

                case 'l':
                    ToggleLog();
                    break;

                case 'v':
                    ToggleHexView();
                    break;

                case 'f':
                    ToggleFreeze();
                    break;

                case 'b':
                    SendBreak();
                    break;

                case 'o':
                    ToggleLocalEcho();
                    break;

                case 's':
                    SendFileCommand();
                    break;

                default:
                    // unknown commands are ignored rather than reported, so a
                    // full screen application on the device is not disturbed
                    if (key.Key == ConsoleKey.F1)
                        DisplayHelp();
                    break;
            }

            return paused;
        }

        private static bool LegacyCommand(ConsoleKeyInfo key, ref bool paused)
        {
            switch (key.Key)
            {
                case ConsoleKey.Home:
                    ClearScreen();
                    return true;

                case ConsoleKey.Escape:
                    _continue = false;
                    return true;

                case ConsoleKey.F1:
                    DisplayHelp();
                    return true;

                case ConsoleKey.F2:
                    paused = TogglePause(paused);
                    return true;

                case ConsoleKey.F3:
                    SayLine($"\r\nConnected to: {SerialPortToString()}");
                    return true;

                case ConsoleKey.F4:
                    ResetEsp32Command();
                    return true;

                case ConsoleKey.F5:
                    paused = PicoProgrammingCommand();
                    return true;

                default:
                    return false;
            }
        }

        private static bool TogglePause(bool paused)
        {
            paused = !paused;

            if (paused)
            {
                Say("\r\nDisconnected ... ");
                _serialPort.Close();
            }
            else
                Say("\r\nReconnecting ... ");

            return paused;
        }

        private static void ResetEsp32Command()
        {
            Say($"\r\nESP32 Soft Reset. Toggling RTS ... ");
            ResetEsp32(100);
            SayLine($"Done ...");
        }

        private static bool PicoProgrammingCommand()
        {
            SayLine($"\r\nPi PICO Programming mode. Connecting 1200 baud ... ");
            PicoProgrammingMode();
            Say("Disconnected ... ");
            return true;
        }

        // Accepts a control key as '^A', 'Ctrl+A', '0x01' or a bare 'A'
        private static char ParseEscapeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DefaultEscapeKey;

            value = value.Trim();

            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                && byte.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte code))
                return (char)code;

            if (value.Length == 2 && value[0] == '^')
                return ControlKey(value[1]);

            if (value.Length == 6 && value.StartsWith("ctrl+", StringComparison.OrdinalIgnoreCase))
                return ControlKey(value[5]);

            if (value.Length == 1)
                return ControlKey(value[0]);

            SayLine($"'{value}' is not a valid escape key, defaulting to Ctrl+A");
            return DefaultEscapeKey;
        }

        private static char ControlKey(char key)
        {
            return (char)(char.ToUpperInvariant(key) & 0x1F);
        }

        private static string EscapeKeyName()
        {
            return _escapeKey < 0x20
                ? $"Ctrl+{(char)(_escapeKey | 0x40)}"
                : _escapeKey.ToString();
        }
    }
}

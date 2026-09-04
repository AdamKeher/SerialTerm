using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace TerminalConsole
{
    partial class Program
    {
        private static void DisplayHelp()
        {
            Console.WriteLine("\r\nTerminal Keys");
            Console.WriteLine("-------------");

            var prefix = EscapeKeyName();

            var rows = new List<string[]>
            {
                new[] { $"{prefix} ?", "Display SerialTerm key help" },
                new[] { $"{prefix} d", "Disconnect / Reconnect serial connection" },
                new[] { $"{prefix} i", "Display serial port settings" },
                new[] { $"{prefix} e", "Soft reset ESP32 by toggling RTS enabled" },
                new[] { $"{prefix} p", "Reset PICO to programming mode by toggling 1200 baud connection" },
                new[] { $"{prefix} c", "Clear terminal screen" },
                new[] { $"{prefix} q", "Exit terminal program" },
                new[] { $"{prefix} {prefix}", $"Send a literal {prefix} to the connected device" },
            };

            if (_legacyKeys)
            {
                rows.Add(new[] { "F1", "Display SerialTerm key help" });
                rows.Add(new[] { "F2", "Disconnect / Reconnect serial connection" });
                rows.Add(new[] { "F3", "Display serial port settings" });
                rows.Add(new[] { "F4", "Soft reset ESP32 by toggling RTS enabled" });
                rows.Add(new[] { "F5", "Reset PICO to programming mode by toggling 1200 baud connection" });
                rows.Add(new[] { "Home", "Clear terminal screen" });
                rows.Add(new[] { "ESC", "Exit terminal program" });
            }

            WriteTable(new[] { "Key", "Function" }, rows);

            Console.WriteLine();
            Console.WriteLine(_legacyKeys
                ? "Every other key is sent to the connected device. ESC and F1-F5 are held by SerialTerm."
                : "Every other key, ESC and Ctrl+C included, is sent to the connected device.");

            Console.WriteLine();
        }

        private static void DisplayPorts()
        {
            string[] portnames = SerialPort.GetPortNames();

            if (portnames.Length == 0)
            {
                Console.WriteLine("No serial ports detected.");
                return;
            }

            var rows = new List<string[]>();

            for (int index = 0; index < portnames.Length; index++)
            {
                bool free;

                using (var port = new SerialPort(portnames[index]))
                {
                    try
                    {
                        port.Open();
                        port.Close();
                        free = true;
                    }
                    catch (Exception)
                    {
                        free = false;
                    }
                }

                rows.Add(new[] { (index + 1).ToString(), portnames[index], free ? "(free)" : "(busy)" });
            }

            WriteTable(new[] { "#", "Port", "Status" }, rows);
        }
    }
}

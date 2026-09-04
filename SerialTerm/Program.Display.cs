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

        // Probing asks each port whether it opens, which reboots anything wired
        // for auto reset. It is opt in for that reason.
        private static void DisplayPorts(bool probe = false)
        {
            string[] portnames = SerialPort.GetPortNames();

            if (portnames.Length == 0)
            {
                Console.WriteLine("No serial ports detected.");
                return;
            }

            Dictionary<string, string> descriptions = GetPortDescriptions();

            var headers = new List<string> { "#", "Port", "Device" };
            if (probe)
                headers.Add("Status");

            var rows = new List<string[]>();

            for (int index = 0; index < portnames.Length; index++)
            {
                string port = portnames[index];

                var row = new List<string>
                {
                    (index + 1).ToString(),
                    port,
                    descriptions.TryGetValue(port, out string description) ? description : "-"
                };

                if (probe)
                    row.Add(IsPortFree(port) ? "(free)" : "(busy)");

                rows.Add(row.ToArray());
            }

            WriteTable(headers, rows);
        }

        private static bool IsPortFree(string portName)
        {
            using var port = new SerialPort(portName);

            try
            {
                port.Open();
                port.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

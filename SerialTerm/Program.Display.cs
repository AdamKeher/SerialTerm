using System;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.IO.Ports;

namespace TerminalConsole
{
    partial class Program
    {
        private static void DisplayHelp()
        {
            Console.WriteLine("\r\nTerminal Keys");
            Console.WriteLine("-------------");

            var consoleRenderer = new ConsoleRenderer(
                _invocationContext.Console,
                _invocationContext.BindingContext.OutputMode(),
                true);

            var prefix = EscapeKeyName();

            var helpList = new List<dynamic>();
            helpList.Add(new { Key = $"{prefix} ?", Function = "Display SerialTerm key help" });
            helpList.Add(new { Key = $"{prefix} d", Function = "Disconnect / Reconnect serial connection" });
            helpList.Add(new { Key = $"{prefix} i", Function = "Display serial port settings" });
            helpList.Add(new { Key = $"{prefix} e", Function = "Soft reset ESP32 by toggling RTS enabled" });
            helpList.Add(new { Key = $"{prefix} p", Function = "Reset PICO to programming mode by toggling 1200 baud connection" });
            helpList.Add(new { Key = $"{prefix} c", Function = "Clear terminal screen" });
            helpList.Add(new { Key = $"{prefix} q", Function = "Exit terminal program" });
            helpList.Add(new { Key = $"{prefix} {prefix}", Function = $"Send a literal {prefix} to the connected device" });

            if (_legacyKeys)
            {
                helpList.Add(new { Key = "F1", Function = "Display SerialTerm key help" });
                helpList.Add(new { Key = "F2", Function = "Disconnect / Reconnect serial connection" });
                helpList.Add(new { Key = "F3", Function = "Display serial port settings" });
                helpList.Add(new { Key = "F4", Function = "Soft reset ESP32 by toggling RTS enabled" });
                helpList.Add(new { Key = "F5", Function = "Reset PICO to programming mode by toggling 1200 baud connection" });
                helpList.Add(new { Key = "Home", Function = "Clear terminal screen" });
                helpList.Add(new { Key = "ESC", Function = "Exit terminal program" });
            }

            var tableView = new TableView<dynamic>
            {
                Items = helpList.ToList()
            };

            tableView.AddColumn(f => f.Key, "Key");
            tableView.AddColumn(f => f.Function, "Function");

            Region region = new Region(0, 0, new Size(Console.WindowWidth, Console.BufferHeight));
            tableView.Render(consoleRenderer, region);
            Console.WriteLine();
            Console.WriteLine(_legacyKeys
                ? "Every other key is sent to the connected device. ESC and F1-F5 are held by SerialTerm."
                : "Every other key, ESC and Ctrl+C included, is sent to the connected device.");

            Console.WriteLine();
        }

        private static void DisplayPorts()
        {
            var consoleRenderer = new ConsoleRenderer(
                _invocationContext.Console,
                _invocationContext.BindingContext.OutputMode(),
                true);

            string[] portnames = SerialPort.GetPortNames();

            if (portnames.Length == 0)
            {
                Console.WriteLine("No serial ports detected.");
                return;
            }

            List<dynamic> serialList = new List<dynamic>();

            int count = 0;
            foreach (var port in portnames)
            {
                _serialPort = new SerialPort();
                _serialPort.PortName = portnames[count];

                bool serialStatus = false;

                try
                {
                    _serialPort.Open();
                    _serialPort.Close();
                }
                catch (Exception)
                {
                    serialStatus = true;
                }

                var serialObject = new { Count = count + 1, Port = portnames[count], Status = !serialStatus ? "(free)" : "(busy)" };
                serialList.Add(serialObject);

                count++;
            }

            var tableView = new TableView<dynamic>
            {
                Items = serialList.ToList()
            };

            tableView.AddColumn(f => f.Count, "#");
            tableView.AddColumn(f => f.Port, "Port");
            tableView.AddColumn(f => f.Status, "Status");

            Region region = new Region(0, 0, new Size(Console.WindowWidth, Console.BufferHeight));
            tableView.Render(consoleRenderer, region);
        }
    }
}

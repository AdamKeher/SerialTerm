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

            var helpList = new List<dynamic>();
            helpList.Add(new { Key = "F1", Function = "Display SerialTerm key help" });
            helpList.Add(new { Key = "F2", Function = "Disconnect / Reconnect serial connection" });
            helpList.Add(new { Key = "F3", Function = "Display serial port settings" });
            helpList.Add(new { Key = "F4", Function = "Soft reset ESP32 by toggling RTS enabled" });
            helpList.Add(new { Key = "F5", Function = "Reset PICO to programming mode by toggling 1200 baud connection" });
            helpList.Add(new { Key = "Home", Function = "Clear terminal screen" });
            helpList.Add(new { Key = "ESC", Function = "Exit terminal program" });

            var tableView = new TableView<dynamic>
            {
                Items = helpList.ToList()
            };

            tableView.AddColumn(f => f.Key, "Key");
            tableView.AddColumn(f => f.Function, "Function");

            Region region = new Region(0, 0, new Size(Console.WindowWidth, Console.BufferHeight));
            tableView.Render(consoleRenderer, region);

            Console.WriteLine();
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

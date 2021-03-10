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
            helpList.Add(new { Key = "F1", Function = "Display SimpleTerm key help" });
            helpList.Add(new { Key = "F2", Function = "Disconnect / Reconnect serial connection" });
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

            //var screen = new ScreenView(consoleRenderer, invocationContext.Console) { Child = tableView };
            //screen.Render();

            Console.WriteLine();
            Console.WriteLine();
        }

        private static void DisplayPorts()
        {
            List<dynamic> serialList = new List<dynamic>();

            string[] portNames = SerialPort.GetPortNames();
            if (portNames.Length == 0)
            {
                Console.WriteLine("No serial ports detected.");
                return;
            }

            int count = 0;
            foreach (var portName in portNames)
            {
                _serialPort = new SerialPort();
                _serialPort.PortName = portName;

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

                var serialObject = new { Count = ++count, Name = portName, Status = !serialStatus ? "(free)" : "(busy)" };
                serialList.Add(serialObject);
            }

            var consoleRenderer = new ConsoleRenderer(
                _invocationContext.Console,
                _invocationContext.BindingContext.OutputMode(),
                true);

            var tableView = new TableView<dynamic>
            {
                Items = serialList.ToList()
            };

            tableView.AddColumn(f => f.Count, "#");
            tableView.AddColumn(f => f.Name, "Name");
            tableView.AddColumn(f => f.Status, "Status");

            Region region = new Region(0, 0, new Size(Console.WindowWidth, Console.BufferHeight));
            tableView.Render(consoleRenderer, region);

            //var screen = new ScreenView(consoleRenderer, invocationContext.Console) { Child = tableView };
            //screen.Render();

            return;
        }
    }
}

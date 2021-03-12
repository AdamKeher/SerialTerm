using System;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.IO.Ports;
using System.Threading;

namespace TerminalConsole
{
    partial class Program
    {
        private static bool ProcessKeys(bool paused)
        {
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Home)
                Console.Clear();

            if (key.Key == ConsoleKey.Escape)
                _continue = false;

            if (key.Key == ConsoleKey.F1)
            {
                DisplayHelp();
            }

            if (key.Key == ConsoleKey.F2)
            {
                paused = !paused;
                if (paused)
                {
                    Console.Write("Disconnected ... ");
                    _serialPort.Close();
                }
                else
                    Console.Write("Reconnecting ... ");
            }

            if (key.Key == ConsoleKey.F3)
            {
                Console.WriteLine($"Connected to: {SerialPortToString()}");
            }


            if (key.Key == ConsoleKey.F4)
            {
                Console.Write($"Toggling RTS to reset... ");
                _serialPort.RtsEnable = true;
                Thread.Sleep(100);
                Console.WriteLine($"Done ...");
                _serialPort.RtsEnable = false;
            }

            return paused;
        }
    }
}

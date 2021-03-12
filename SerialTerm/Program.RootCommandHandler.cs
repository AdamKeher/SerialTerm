using System;
using System.CommandLine.Invocation;
using System.IO.Ports;
using System.Threading;

namespace TerminalConsole
{
    partial class Program
    {
        static void RootCommmandHandler(InvocationContext context, CommandLineOptions options)
        {
            _invocationContext = context;

            _serialPort = GetSerialPort(options);

            // open serial port
            Console.WriteLine("Connecting to: {0}", SerialPortToString());

            try
            {
                _serialPort.Open();
            }
            catch (Exception)
            {
                Console.WriteLine($"Failed to open {_serialPort.PortName}");
            }

            // wait while receiving data and handle disconnection and control keys
            bool reconnecting = false;
            bool paused = false;
            _continue = true;
            while (_continue)
            {
                // handle serial disconnection and reconnection
                if (!paused && !_serialPort.IsOpen)
                {
                    try
                    {
                        _serialPort.Open();
                        reconnecting = false;
                        Console.WriteLine("Reconnected.");
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        if (!reconnecting) Console.WriteLine("Disconnected.");
                        if (options.disconnectExit)
                            return;
                        reconnecting = true;
                    }
                    catch (System.IO.IOException) { }
                }

                // control keys
                if (Console.KeyAvailable)
                {
                    paused = ProcessKeys(paused);
                }

                Thread.Sleep(100);
            }
        }
    }
}

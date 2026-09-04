using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace TerminalConsole
{
    partial class Program
    {
        // how long to wait between scans while no COM device is present
        private const int PortScanInterval = 250;

        // returns null when no port was chosen, so the caller can exit quietly
        private static SerialPort GetSerialPort(CommandLineOptions options)
        {
            string portName = options.port ?? GetPortName();
            if (portName == null)
                return null;

            // setup serial port
            SerialPort serialPort = new SerialPort()
            {
                PortName = portName,
                BaudRate = options.baud,
                DataBits = options.dataBits,
                Parity = options.parity,
                StopBits = options.stopBits,
                Handshake = options.handshake,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            serialPort.DtrEnable = !options.dtr;
            serialPort.RtsEnable = !options.rts;
            serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            serialPort.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorReceivedHandler);

            return serialPort;
        }

        private static string GetPortName()
        {
            int portIndex = -1;
            bool waiting = false;
            string[] ports;

            do
            {
                ports = SerialPort.GetPortNames();

                if (ports.Length == 0)
                {
                    if (!waiting) Console.WriteLine("Waiting for COM device.");
                    waiting = true;

                    // enumerating ports hits the registry, so pause between
                    // scans rather than spinning a core while the user goes
                    // looking for a cable
                    Thread.Sleep(PortScanInterval);
                    continue;
                }

                if (ports.Length == 1)
                {
                    portIndex = 0;
                    Console.WriteLine("Port defaulted to {0}", ports[portIndex]);
                }
                else
                {
                    Console.WriteLine("Select a port:");

                    DisplayPorts();
                    Console.WriteLine();
                    Console.Write($"port number (1-{ports.Length}, q to quit): ");

                    string entry = Console.ReadLine()?.Trim();

                    // end of input or an explicit quit, the caller gives up
                    if (entry == null || entry.Equals("q", StringComparison.OrdinalIgnoreCase))
                        return null;

                    if (!int.TryParse(entry, out int selection))
                        Console.WriteLine($"'{entry}' is not a number.");
                    else if (selection < 1 || selection > ports.Length)
                        Console.WriteLine($"{selection} is out of range, pick a number between 1 and {ports.Length}.");
                    else
                    {
                        portIndex = selection - 1;
                        Console.WriteLine("Port set to {0}", ports[portIndex]);
                    }
                }
            } while (portIndex == -1);

            return ports[portIndex];
        }

        private static string SerialPortToString()
        {
            return String.Format("'{0}' (B:{1} | P:{2} | DB: {3} | SB:{4} | HS: {5} | DTR {6} | RTS {7}) ",
                _serialPort.PortName,
                _serialPort.BaudRate,
                _serialPort.Parity.ToString(),
                _serialPort.DataBits,
                _serialPort.StopBits.ToString(),
                _serialPort.Handshake.ToString(),
                _serialPort.DtrEnable,
                _serialPort.RtsEnable);
        }

        private static void ResetEsp32(int duration)
        {
            _serialPort.RtsEnable = true;
            Thread.Sleep(duration);
            _serialPort.RtsEnable = false;
        }

        private static void PicoProgrammingMode()
        {
            _serialPort.Close();
            int oldbaud = _serialPort.BaudRate;
            _serialPort.BaudRate = 1200;
            try
            {
                _serialPort.Open();
                Thread.Sleep(500);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: Unable to open / find port ... ");
            }
            _serialPort.Close();
            _serialPort.BaudRate = oldbaud;

        }

        private static void ErrorReceivedHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort port = (SerialPort)sender;
            Console.WriteLine("{0} Error: {1}", port.PortName, e.EventType.ToString());
        }

        // Device output is passed straight through as bytes. Decoding it as text
        // first would corrupt anything the device sends outside plain ASCII and
        // is not needed - the console interprets the escape sequences itself.
        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = (SerialPort)sender;

            try
            {
                int waiting = port.BytesToRead;
                if (waiting <= 0)
                    return;

                byte[] buffer = new byte[waiting];
                int read = port.Read(buffer, 0, waiting);
                if (read <= 0)
                    return;

                lock (_consoleLock)
                {
                    bool hint = _hintVisible;
                    if (hint) HideHint();

                    _standardOutput ??= Console.OpenStandardOutput();
                    _standardOutput.Write(buffer, 0, read);
                    _standardOutput.Flush();

                    if (hint) ShowHint();
                }
            }
            catch (TimeoutException) { }
            catch (InvalidOperationException) { }
            catch (IOException) { }
        }

        private static void SendToPort(byte[] data)
        {
            if (data == null || data.Length == 0 || !_serialPort.IsOpen)
                return;

            try
            {
                _serialPort.Write(data, 0, data.Length);
            }
            catch (TimeoutException) { }
            catch (InvalidOperationException) { }
            catch (IOException) { }
        }
    }
}

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace TerminalConsole
{
    class Program
    {
        static SerialPort _serialPort;
        static bool _continue;

        /// <summary>
        /// SimpleTerm - Simple serial port terminal program.
        /// (c)2021 AKsevenFour - https://github.com/AdamKeher/SimpleTerm
        /// </summary>
        ///  <param name="invocationContext"></param>
        ///  <param name="listPorts">List all serial ports</param>
        ///  <param name="port">Set the serial port to listen on</param>
        ///  <param name="baud">Set serial port baud rate</param>
        ///  <param name="dataBits">Sets the standard length of data bits per byte (5..[8])</param>
        ///  <param name="parity">Sets the parity-checking protocol ([None] | Mark | Even | Odd | Space)</param>
        ///  <param name="stopBits">Sets the standard number of stopbits per byte ([One] | OnePointFive | Two | )</param>
        ///  <param name="handshake">Specifies the control protocol used in establishing a serial port communication ([None] | RTS | XonXoff | RTSXonXoff)</param>
        ///  <param name="disconnectExit">Exit terminal on disconnection</param>
        static void Main(
            InvocationContext invocationContext,
            bool listPorts = false,
            string port = null,
            int baud = 115200,
            int dataBits = 8,
            string parity = "none",
            string stopBits = "one",
            string handshake = "none",
            bool disconnectExit = false)
        {
            var command = new RootCommand
            {
                new Option(new [] {"--AdamTest", "-at"}),
            };

            Console.WriteLine("\u001b[31mSerial\u001b[31;1mTERM\u001b[37m v0.2 (c)2021 \u001b[32m\u001b[7mAKsevenFour\u001b[0m.");

            if (listPorts)
            {
                ListPorts(invocationContext);
                return;
            }

            // validate inputs
            if (dataBits < 5 || dataBits > 8)
            {
                Console.WriteLine("Invalid data-bits value, the correct range is (5..[8])");
                return;
            }

            List<string> _validOptions = new List<string>() { "none", "mark", "even", "odd", "space" };
            if (!_validOptions.Contains(parity.ToLower()))
            {
                Console.WriteLine("Invalid parity value, the correct values are ([none] | mark | even | odd | space)");
                return;
            }

            _validOptions.Clear();
            _validOptions.AddRange(new String[] { "one", "onepointfive", "two" });
            if (!_validOptions.Contains(stopBits.ToLower()))
            {
                Console.WriteLine("Invalid stop-bits value, the correct values are ([one] | onepointfive | two)");
                return;
            }

            _validOptions.Clear();
            _validOptions.AddRange(new String[] { "none", "xonxoff", "rts", "rtsxonxoff" });
            if (!_validOptions.Contains(handshake.ToLower()))
            {
                Console.WriteLine("Invalid handshake value, the correct values are ([none] | xonxoff | rts | rtsxonxoff)");
                return;
            }

            // setup serial port
            _serialPort = new SerialPort();
            _serialPort.PortName = port ?? SetPortName(invocationContext);
            _serialPort.BaudRate = baud;
            _serialPort.DataBits = dataBits;
            _serialPort.Parity = SetParity(parity);
            _serialPort.StopBits = SetStopBits(stopBits);
            _serialPort.Handshake = SetHandshake(handshake);
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            _serialPort.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorReceivedHandler);

            // open serial port
            Console.WriteLine("Connecting to: {0}", SerialPortDetails(_serialPort));

            try
            {
                _serialPort.Open();

            }
            catch (Exception)
            {
                Console.WriteLine($"Failed to open {_serialPort.PortName}");
            }

            // wait while receiving data and handle disconnection and control keys
            bool _reconnecting = false;
            bool _paused = false;
            _continue = true;
            while (_continue)
            {
                // handle serial disconnection and reconnection
                if (!_paused && !_serialPort.IsOpen)
                {
                    try
                    {
                        _serialPort.Open();
                        _reconnecting = false;
                        Console.WriteLine("Reconnected.");
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        if (!_reconnecting) Console.WriteLine("Disconnected.");
                        if (disconnectExit)
                            return;
                        _reconnecting = true;
                    }
                    catch (System.IO.IOException) { }

                }

                // control keys
                if (Console.KeyAvailable)
                {
                    _paused = processKeys(invocationContext, _paused);
                }

                Thread.Sleep(100);
            }
        }

        private static bool processKeys(InvocationContext invocationContext, bool _paused)
        {
            var _key = Console.ReadKey(true);

            if (_key.Key == ConsoleKey.Home)
                Console.Clear();
            
            if (_key.Key == ConsoleKey.Escape)
                _continue = false;
            
            if (_key.Key == ConsoleKey.F1)
            {
                displayHelp(invocationContext);
            }

            if (_key.Key == ConsoleKey.F2)
            {
                _paused = !_paused;
                if (_paused)
                {
                    Console.Write("Disconnected ... ");
                    _serialPort.Close();
                }
                else
                    Console.Write("Reconnecting ... ");
            }

            return _paused;
        }

        private static void displayHelp(InvocationContext invocationContext)
        {
            Console.WriteLine("\r\nTerminal Keys");
            Console.WriteLine("-------------");

            var consoleRenderer = new ConsoleRenderer(
                invocationContext.Console,
                invocationContext.BindingContext.OutputMode(),
                true);

            var _helpList = new List<dynamic>();
            _helpList.Add(new { Key = "F1", Function = "Display SimpleTerm key help" });
            _helpList.Add(new { Key = "F2", Function = "Disconnect / Reconnect serial connection" });
            _helpList.Add(new { Key = "Home", Function = "Clear terminal screen" });
            _helpList.Add(new { Key = "ESC", Function = "Exit terminal program" });

            var _tableView = new TableView<dynamic>
            {
                Items = _helpList.ToList()
            };

            _tableView.AddColumn(f => f.Key, "Key");

            _tableView.AddColumn(f => f.Function, "Function");

            var screen = new ScreenView(consoleRenderer, invocationContext.Console) { Child = _tableView };
            screen.Render();
        
            Console.WriteLine();
            Console.WriteLine();
        }

        private static Handshake SetHandshake(string handshake)
        {
            switch (handshake.ToLower())
            {
                case "none":
                    return Handshake.None;
                case "xonxoff":
                    return Handshake.XOnXOff;
                case "rts":
                    return Handshake.RequestToSend;
                case "rtsxonxoff":
                    return Handshake.RequestToSendXOnXOff;
                default:
                    return Handshake.None;
            }
        }
        private static StopBits SetStopBits(string stopBits)
        {
            switch (stopBits.ToLower())
            {
                case "one":
                    return StopBits.One;
                case "onepointfive":
                    return StopBits.OnePointFive;
                case "two":
                    return StopBits.Two;
                default:
                    return StopBits.One;
            }
        }
        private static Parity SetParity(string parity)
        {
            switch (parity.ToLower())
            {
                case "none":
                    return Parity.None;
                case "even":
                    return Parity.Even;
                case "mark":
                    return Parity.Mark;
                case "odd":
                    return Parity.Odd;
                case "space":
                    return Parity.Space;
                default:
                    return Parity.None;
            }
        }
        private static string SetPortName(InvocationContext invocationContext)
        {
            int _portIndex = -1;
            bool _waiting = false;
            string[] _ports;

            do
            {
                _ports = SerialPort.GetPortNames();

                if (_ports.Length == 0)
                {
                    if (!_waiting) Console.WriteLine("Waiting for COM device.");
                    _waiting = true;
                }
                else if (_ports.Length == 1)
                {
                    _portIndex = 0;
                    Console.WriteLine("Port defaulted to {0}", _ports[_portIndex]);
                }
                else
                {
                    Console.WriteLine("Select a port:");

                    ListPorts(invocationContext);
                    Console.WriteLine();
                    Console.Write("port number: ");

                    var _key = Console.ReadKey(false);
                    Console.WriteLine();

                    try
                    {
                        _portIndex = int.Parse(_key.KeyChar.ToString()) - 1;
                        Console.WriteLine("Port set to {0}", _ports[_portIndex]);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error setting port");
                        _portIndex = -1;
                    }
                }
            } while (_portIndex == -1);

            return _ports[_portIndex];
        }

        private static string SerialPortDetails(SerialPort serialPort)
        {
            return String.Format("'{0}' (B:{1} | P:{2} | DB: {3} | SB:{4} | HS: {5}) ",
                serialPort.PortName,
                serialPort.BaudRate,
                serialPort.Parity.ToString(),
                serialPort.DataBits,
                serialPort.StopBits.ToString(),
                serialPort.Handshake.ToString());
        }

        private static void ListPorts(InvocationContext invocationContext)
        {
            List<dynamic> _serialList = new List<dynamic>();

            string[] _portNames = SerialPort.GetPortNames();
            if (_portNames.Length == 0)
            {
                Console.WriteLine("No serial ports detected.");
                return;
            }

            int count = 0;
            foreach (var portName in _portNames)
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
                _serialList.Add(serialObject);
            }

            var consoleRenderer = new ConsoleRenderer(
                invocationContext.Console,
                invocationContext.BindingContext.OutputMode(),
                true);

            var _tableView = new TableView<dynamic>
            {
                Items = _serialList.ToList()
            };

            _tableView.AddColumn(f => f.Count, "#");

            _tableView.AddColumn(f => f.Name, "Name");

            _tableView.AddColumn(f => f.Status, "Status");

            var screen = new ScreenView(consoleRenderer, invocationContext.Console) { Child = _tableView };
            screen.Render();

            return;
        }

        private static void ErrorReceivedHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort _port = (SerialPort)sender;
            Console.WriteLine("{0} Error: {1}", _port.PortName, e.EventType.ToString());
        }
        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort _port = (SerialPort)sender;
            string _data = _port.ReadExisting();
            Console.Write(_data);
        }
    }
    }

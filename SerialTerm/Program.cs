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
        private static InvocationContext _invocationContext;

        public static int Main(string[] args)
        {
            // create a root command with some options
            var rootCommand = CreateRootCommand(
                "rootCommand",
                "SimpleTerm - Simple serial port terminal program. (c)2021 AKsevenFour - https://github.com/AdamKeher/SerialTerm",
                RootCommmandHandler);

            // create list ports command
            rootCommand.AddCommand(
                new Command("list", "List all serial ports")
                {
                    Handler = CommandHandler.Create((Action<InvocationContext>)(ListCommmandHandler))
                });

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }

        private static RootCommand CreateRootCommand(string name, string description, Action<InvocationContext, ConnectionConfig> action)
        {
            var rootCommand = new RootCommand(name);
            rootCommand.Description = description;
            rootCommand.Handler = CommandHandler.Create(action);

            rootCommand.AddOption(new Option<string>(
                new string[] { "--port", "-P" },
                "Set the serial port to listen on"));

            rootCommand.AddOption(new Option<int>(
                    new string[] { "--baud", "-b" },
                    getDefaultValue: () => 115200,
                    "Set serial port baud rate"));

            rootCommand.AddOption(new Option<bool>(
                    new string[] { "--disconnect-exit", "-de" },
                    getDefaultValue: () => false,
                    "Exit terminal on disconnection"));

            var dbOption = new Option<int>(
                new string[] { "--data-bits", "-db" },
                getDefaultValue: () => 8,
                "Sets the standard length of data bits per byte");
            dbOption.AddSuggestions("5", "6", "7", "8");
            dbOption.AddValidator(optionResult => {
                var suggestions = optionResult.Option.GetSuggestions().ToList();
                if (optionResult.Tokens.Count > 0 && (!suggestions.Any(s => s.Equals(optionResult.Tokens[0].Value.ToLower(), StringComparison.OrdinalIgnoreCase))))
                {
                    return $"{optionResult.Tokens[0].Value} is not a valid argument for {optionResult.Token}";
                }
                return null;
            });
            rootCommand.AddOption(dbOption);

            var parityOption = new Option<string>(
                new string[] { "--parity", "-pa" },
                getDefaultValue: () => "None",
                "Sets the parity-checking protocol");
            parityOption.AddSuggestions("None", "Mark", "Even", "Odd", "Space");
            parityOption.AddValidator(optionResult =>
            {
                var suggestions = optionResult.Option.GetSuggestions().ToList();
                if (optionResult.Tokens.Count > 0 && (!suggestions.Any(s => s.Equals(optionResult.Tokens[0].Value.ToLower(), StringComparison.OrdinalIgnoreCase))))
                {
                    return $"{optionResult.Tokens[0].Value} is not a valid argument for {optionResult.Token}";
                }
                return null;
            });
            rootCommand.AddOption(parityOption);

            var sbOption = new Option<string>(
                new string[] { "--stop-bits", "-sb" },
                getDefaultValue: () => "One",
                "Sets the standard number of stopbits per byte");
            sbOption.AddSuggestions("One", "OnePointFive", "Two");
            sbOption.AddValidator(optionResult =>
            {
                var suggestions = optionResult.Option.GetSuggestions().ToList();
                if (optionResult.Tokens.Count > 0 && (!suggestions.Any(s => s.Equals(optionResult.Tokens[0].Value.ToLower(), StringComparison.OrdinalIgnoreCase))))
                {
                    return $"{optionResult.Tokens[0].Value} is not a valid argument for {optionResult.Token}";
                }
                return null;
            });
            rootCommand.AddOption(sbOption);

            var hsOption = new Option<string>(
                new string[] { "--handshake", "-hs" },
                getDefaultValue: () => "None",
                "Specifies the control protocol used in establishing a serial port communication");
            hsOption.AddSuggestions("None", "RTS", "XonXoff", "RTSXonXoff");
            hsOption.AddValidator(optionResult =>
            {
                var suggestions = optionResult.Option.GetSuggestions().ToList();
                if (optionResult.Tokens.Count > 0 && (!suggestions.Any(s => s.Equals(optionResult.Tokens[0].Value.ToLower(), StringComparison.OrdinalIgnoreCase))))
                {
                    return $"{optionResult.Tokens[0].Value} is not a valid argument for {optionResult.Token}";
                }
                return null;
            });
            rootCommand.AddOption(hsOption);

            return rootCommand;
        }

        static void ListCommmandHandler(InvocationContext context)
        {
            _invocationContext = context;

            Console.WriteLine("Serial Ports");
            Console.WriteLine("------------");
            ListPorts();
        }
        
        static void RootCommmandHandler(InvocationContext context, ConnectionConfig config)
        {
            _invocationContext = context;

            // setup serial port
            _serialPort = new SerialPort()
            {
                PortName = config.port ?? SetPortName(),
                BaudRate = config.baud,
                DataBits = config.dataBits,
                Parity = SetParity(config.parity),
                StopBits = SetStopBits(config.stopBits),
                Handshake = SetHandshake(config.handshake),
                ReadTimeout = 500,
                WriteTimeout = 500
            };
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            _serialPort.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorReceivedHandler);

            // open serial port
            Console.WriteLine("Connecting to: {0}", SerialPortDetails());

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
                        if (config.disconnectExit)
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

            return paused;
        }

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

        private static Handshake SetHandshake(string handshake)
        {
            return (handshake.ToLower()) switch
            {
                "none" => Handshake.None,
                "xonxoff" => Handshake.XOnXOff,
                "rts" => Handshake.RequestToSend,
                "rtsxonxoff" => Handshake.RequestToSendXOnXOff,
                _ => Handshake.None,
            };
        }
        
        private static StopBits SetStopBits(string stopBits)
        {
            return (stopBits.ToLower()) switch
            {
                "one" => StopBits.One,
                "onepointfive" => StopBits.OnePointFive,
                "two" => StopBits.Two,
                _ => StopBits.One,
            };
        }
        
        private static Parity SetParity(string parity)
        {
            return (parity.ToLower()) switch
            {
                "none" => Parity.None,
                "even" => Parity.Even,
                "mark" => Parity.Mark,
                "odd" => Parity.Odd,
                "space" => Parity.Space,
                _ => Parity.None,
            };
        }
        
        private static string SetPortName()
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
                }
                else if (ports.Length == 1)
                {
                    portIndex = 0;
                    Console.WriteLine("Port defaulted to {0}", ports[portIndex]);
                }
                else
                {
                    Console.WriteLine("Select a port:");

                    ListPorts();
                    Console.WriteLine();
                    Console.Write("port number: ");

                    var key = Console.ReadKey(false);
                    Console.WriteLine();

                    try
                    {
                        portIndex = int.Parse(key.KeyChar.ToString()) - 1;
                        Console.WriteLine("Port set to {0}", ports[portIndex]);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error setting port");
                        portIndex = -1;
                    }
                }
            } while (portIndex == -1);

            return ports[portIndex];
        }

        private static string SerialPortDetails()
        {
            return String.Format("'{0}' (B:{1} | P:{2} | DB: {3} | SB:{4} | HS: {5}) ",
                _serialPort.PortName,
                _serialPort.BaudRate,
                _serialPort.Parity.ToString(),
                _serialPort.DataBits,
                _serialPort.StopBits.ToString(),
                _serialPort.Handshake.ToString());
        }

        private static void ListPorts()
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

            Region region = new Region(0,0,new Size(Console.WindowWidth,Console.BufferHeight));
            tableView.Render(consoleRenderer, region);

            //var screen = new ScreenView(consoleRenderer, invocationContext.Console) { Child = tableView };
            //screen.Render();

            return;
        }

        private static void ErrorReceivedHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort port = (SerialPort)sender;
            Console.WriteLine("{0} Error: {1}", port.PortName, e.EventType.ToString());
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = (SerialPort)sender;
            string data = port.ReadExisting();
            Console.Write(data);
        }
    }
}

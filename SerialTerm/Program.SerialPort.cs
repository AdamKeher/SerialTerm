using System;
using System.IO.Ports;

namespace TerminalConsole
{
    partial class Program
    {
        private static SerialPort GetSerialPort(CommandLineOptions options)
        {
            // setup serial port
            SerialPort serialPort = new SerialPort()
            {
                PortName = options.port ?? GetPortName(),
                BaudRate = options.baud,
                DataBits = options.dataBits,
                Parity = (options.parity.ToLower()) switch
                {
                    "none" => Parity.None,
                    "even" => Parity.Even,
                    "mark" => Parity.Mark,
                    "odd" => Parity.Odd,
                    "space" => Parity.Space,
                    _ => Parity.None,
                },
                StopBits = (options.stopBits.ToLower()) switch
                {
                    "one" => StopBits.One,
                    "onepointfive" => StopBits.OnePointFive,
                    "two" => StopBits.Two,
                    _ => StopBits.One,
                },
                Handshake = (options.handshake.ToLower()) switch
                {
                    "none" => Handshake.None,
                    "xonxoff" => Handshake.XOnXOff,
                    "rts" => Handshake.RequestToSend,
                    "rtsxonxoff" => Handshake.RequestToSendXOnXOff,
                    _ => Handshake.None,
                },
                ReadTimeout = 500,
                WriteTimeout = 500
            };

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
                }
                else if (ports.Length == 1)
                {
                    portIndex = 0;
                    Console.WriteLine("Port defaulted to {0}", ports[portIndex]);
                }
                else
                {
                    Console.WriteLine("Select a port:");

                    DisplayPorts();
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

        private static string SerialPortToString()
        {
            return String.Format("'{0}' (B:{1} | P:{2} | DB: {3} | SB:{4} | HS: {5} | DTR {6} | RTS {7})",
                _serialPort.PortName,
                _serialPort.BaudRate,
                _serialPort.Parity.ToString(),
                _serialPort.DataBits,
                _serialPort.StopBits.ToString(),
                _serialPort.Handshake.ToString(),
                _serialPort.DtrEnable,
                _serialPort.RtsEnable);                
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

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Ports;
using System.Threading;

namespace TerminalConsole
{
    partial class Program
    {
        static SerialPort _serialPort;
        static bool _continue;
        private static InvocationContext _invocationContext;

        public static int Main(string[] args)
        {
            // create a root command with some options
            var rootCommand = GetRootCommand(
                "rootCommand",
                "SerialTerm - Simple serial port terminal program. (c)2021 AKsevenFour - https://github.com/AdamKeher/SerialTerm",
                RootCommmandHandler);

            // create list ports command
            var listCommand = new Command("list", "List all serial ports");
            listCommand.SetHandler(ListCommmandHandler);
            rootCommand.Add(listCommand);
            
            // Parse the incoming args and invoke the handler
            var result = rootCommand.InvokeAsync(args).Result;
            return result;
        }
    }
}

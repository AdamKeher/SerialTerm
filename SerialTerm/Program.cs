using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace TerminalConsole
{
    partial class Program
    {
        internal static SerialPort _serialPort;
        static bool _continue;
        static readonly object _consoleLock = new object();
        internal static Stream _standardOutput;

        public static int Main(string[] args)
        {
            // create a root command with some options
            var rootCommand = GetRootCommand(
                "rootCommand",
                "SerialTerm - Simple serial port terminal program. (c)2021 AKsevenFour - https://github.com/AdamKeher/SerialTerm",
                RootCommmandHandler);

            // create list ports command
            var listCommand = new Command("list", "List all serial ports");
            var probeOption = new Option<bool>(
                new string[] { "--probe", "-p" },
                getDefaultValue: () => false,
                "Also report whether each port is free, by opening it. This resets devices wired for auto reset");
            listCommand.AddOption(probeOption);
            listCommand.SetHandler(ListCommmandHandler, probeOption);
            rootCommand.Add(listCommand);

            // create profiles command
            var profilesCommand = new Command("profiles", "List saved argument profiles");
            profilesCommand.SetHandler(ProfilesCommandHandler);
            rootCommand.Add(profilesCommand);
            
            // Parse the incoming args and invoke the handler
            return rootCommand.Invoke(ResolveProfiles(args, rootCommand));
        }
    }
}

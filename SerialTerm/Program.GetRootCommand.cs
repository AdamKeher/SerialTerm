﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;

namespace TerminalConsole
{
    partial class Program
    {
        public static RootCommand GetRootCommand(string name, string description, Action<InvocationContext, CommandLineOptions> action)
        {
            var rootCommand = new RootCommand(name);
            rootCommand.Description = description;

            var portOption = new Option<string>(
                new string[] { "--port", "-P" },
                "Set the serial port to listen on");
            rootCommand.AddOption(portOption);

            var baudOption = new Option<int>(
                    new string[] { "--baud", "-b" },
                    getDefaultValue: () => 115200,
                    "Set serial port baud rate");
            rootCommand.AddOption(baudOption);

            var disconnectExitOpen = new Option<bool>(
                    new string[] { "--disconnect-exit", "-de" },
                    getDefaultValue: () => false,
                    "Exit terminal on disconnection");
            rootCommand.AddOption(disconnectExitOpen);

            var resetEsp32Option = new Option<bool>(
                    new string[] { "--reset-esp32", "-r" },
                    getDefaultValue: () => false,
                    "Reset ESP32 on connection");
            rootCommand.AddOption(resetEsp32Option);

            var disableDTROption = new Option<bool>(
                    new string[] { "--disable-dtr", "-dtr" },
                    getDefaultValue: () => false,
                    "Disable DTR for serial connection");
            rootCommand.AddOption(disableDTROption);

            var disableRTSOption = new Option<bool>(
                    new string[] { "--disable-rts", "-rts" },
                    getDefaultValue: () => false,
                    "Disable RTS for serial connection");
            rootCommand.AddOption(disableRTSOption);

            var dbOption = new Option<int>(
                new string[] { "--data-bits", "-db" },
                getDefaultValue: () => 8,
                "Sets the standard length of data bits per byte");
            dbOption.AddCompletions("5", "6", "7", "8");
            dbOption.AddValidator(optionResult => {
                var suggestions = optionResult.Option.AddCompletions().Aliases;
                if (optionResult.Tokens.Count > 0 && (!suggestions.Any(s => s.Equals(optionResult.Tokens[0].Value.ToLower(), StringComparison.OrdinalIgnoreCase))))
                {
                    Console.WriteLine($"{optionResult.Tokens[0].Value} is not a valid argument for {optionResult.Token}");
                     return;
                }
                return ;
            });
            rootCommand.AddOption(dbOption);

            var parityOption = new Option<string>(
                new string[] { "--parity", "-pa" },
                getDefaultValue: () => "None",
                "Sets the parity-checking protocol");
            parityOption.AddCompletions("None", "Mark", "Even", "Odd", "Space");
            parityOption.AddValidator(optionResult =>
            {
                var suggestions = optionResult.Option.AddCompletions().Aliases;
                if (optionResult.Tokens.Count > 0 && (!suggestions.Any(s => s.Equals(optionResult.Tokens[0].Value.ToLower(), StringComparison.OrdinalIgnoreCase))))
                {
                    Console.WriteLine( $"{optionResult.Tokens[0].Value} is not a valid argument for {optionResult.Token}");
                    return ;
                }
                return;
            });
            rootCommand.AddOption(parityOption);

            var sbOption = new Option<string>(
                new string[] { "--stop-bits", "-sb" },
                getDefaultValue: () => "One",
                "Sets the standard number of stopbits per byte");
            sbOption.AddCompletions("One", "OnePointFive", "Two");
            sbOption.AddValidator(optionResult =>
            {
                var suggestions = optionResult.Option.AddCompletions().Aliases;
                if (optionResult.Tokens.Count > 0 && (!suggestions.Any(s => s.Equals(optionResult.Tokens[0].Value.ToLower(), StringComparison.OrdinalIgnoreCase))))
                {
                     Console.WriteLine($"{optionResult.Tokens[0].Value} is not a valid argument for {optionResult.Token}");
                     return;
                }
                return ;
            });
            rootCommand.AddOption(sbOption);

            var hsOption = new Option<string>(
                new string[] { "--handshake", "-hs" },
                getDefaultValue: () => "None",
                "Specifies the control protocol used in establishing a serial port communication");
            hsOption.AddCompletions("None", "RTS", "XonXoff", "RTSXonXoff");
            hsOption.AddValidator(optionResult =>
            {
                var suggestions = optionResult.Option.AddCompletions().Aliases;
                if (optionResult.Tokens.Count > 0 && (!suggestions.Any(s => s.Equals(optionResult.Tokens[0].Value.ToLower(), StringComparison.OrdinalIgnoreCase))))
                {
                      Console.WriteLine($"{optionResult.Tokens[0].Value} is not a valid argument for {optionResult.Token}");
                     return;
                }
                return;
            });
            rootCommand.AddOption(hsOption);


            rootCommand.SetHandler((context)=>
                {
                    var opts = new CommandLineOptions(){
                        baud = context.ParseResult.GetValueForOption(baudOption),
                        dataBits = context.ParseResult.GetValueForOption(dbOption),
                        disconnectExit = context.ParseResult.GetValueForOption(disconnectExitOpen),
                        dtr = context.ParseResult.GetValueForOption(disableDTROption),
                        handshake = context.ParseResult.GetValueForOption(hsOption),
                        parity = context.ParseResult.GetValueForOption(parityOption),
                        port = context.ParseResult.GetValueForOption(portOption),
                        resetEsp32 = context.ParseResult.GetValueForOption(resetEsp32Option),
                        rts = context.ParseResult.GetValueForOption(disableRTSOption),
                        stopBits = context.ParseResult.GetValueForOption(sbOption)
                    };
                    action.Invoke(context, opts);
                }
            );
            
            return rootCommand;
        }
    }
}

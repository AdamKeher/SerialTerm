using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Ports;

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

            var matchOption = new Option<string>(
                    new string[] { "--match", "-M" },
                    "Pick the port whose name or device description contains this text, eg. --match CP210x");
            rootCommand.AddOption(matchOption);

            var baudOption = new Option<int>(
                    new string[] { "--baud", "-b" },
                    getDefaultValue: () => 115200,
                    "Set serial port baud rate");
            baudOption.AddValidator(optionResult =>
            {
                if (optionResult.Tokens.Count == 0)
                    return;

                string value = optionResult.Tokens[0].Value;
                if (!int.TryParse(value, out int baud) || baud <= 0)
                    optionResult.ErrorMessage = $"'{value}' is not a valid baud rate, it must be a positive number.";
            });
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

            var escapeKeyOption = new Option<string>(
                    new string[] { "--escape-key", "-ek" },
                    getDefaultValue: () => "^A",
                    "Set the escape key used to reach SerialTerm commands, eg. ^A, ^], 0x1D");
            rootCommand.AddOption(escapeKeyOption);

            var legacyKeysOption = new Option<bool>(
                    new string[] { "--legacy-keys", "-lk" },
                    getDefaultValue: () => false,
                    "Also bind the original F1-F5, Home and ESC keys, ESC will not reach the device");
            rootCommand.AddOption(legacyKeysOption);

            var noHintOption = new Option<bool>(
                    new string[] { "--no-hint", "-nh" },
                    getDefaultValue: () => false,
                    "Do not show the command hint line while the escape key is pending");
            rootCommand.AddOption(noHintOption);

            var noUtf8Option = new Option<bool>(
                    new string[] { "--no-utf8", "-nu" },
                    getDefaultValue: () => false,
                    "Leave the console output code page alone, for a device that sends its own 8 bit encoding rather than UTF-8");
            rootCommand.AddOption(noUtf8Option);

            var statusLineOption = new Option<bool>(
                    new string[] { "--status-line", "-sl" },
                    getDefaultValue: () => false,
                    "Reserve the bottom row for a status line. Costs the device one row of screen");
            rootCommand.AddOption(statusLineOption);

            var logOption = new Option<string>(
                    new string[] { "--log", "-l" },
                    "Append everything the device sends to a file. Ctrl+A l starts and stops it during a session");
            rootCommand.AddOption(logOption);

            var logRawOption = new Option<bool>(
                    new string[] { "--log-raw", "-lr" },
                    getDefaultValue: () => false,
                    "Keep ANSI escape sequences in the log instead of stripping them");
            rootCommand.AddOption(logRawOption);

            var sendFileOption = new Option<string>(
                    new string[] { "--send-file", "-sf" },
                    "Send a text file to the device line by line after connecting. Ctrl+A s sends one during a session");
            rootCommand.AddOption(sendFileOption);

            var sendDelayOption = new Option<int>(
                    new string[] { "--send-delay", "-sd" },
                    getDefaultValue: () => 0,
                    "Milliseconds to pause between lines when sending a file");
            sendDelayOption.AddValidator(optionResult =>
            {
                if (optionResult.Tokens.Count == 0)
                    return;

                string value = optionResult.Tokens[0].Value;
                if (!int.TryParse(value, out int delay) || delay < 0)
                    optionResult.ErrorMessage = $"'{value}' is not a valid send delay, it must be zero or more milliseconds.";
            });
            rootCommand.AddOption(sendDelayOption);

            var sendWaitOption = new Option<string>(
                    new string[] { "--send-wait", "-sw" },
                    "Wait for this text from the device after each line, eg. the REPL prompt");
            rootCommand.AddOption(sendWaitOption);

            var sendTimeoutOption = new Option<int>(
                    new string[] { "--send-timeout", "-st" },
                    getDefaultValue: () => 2000,
                    "How long to wait for --send-wait before giving up, in milliseconds");
            rootCommand.AddOption(sendTimeoutOption);

            var macroOption = new Option<string[]>(
                    new string[] { "--macro", "-m" },
                    @"Bind text to Ctrl+A 1 through Ctrl+A 9, eg. --macro 1=reboot\r. Repeatable");
            macroOption.AllowMultipleArgumentsPerToken = false;
            rootCommand.AddOption(macroOption);

            var localEchoOption = new Option<bool>(
                    new string[] { "--local-echo", "-le" },
                    getDefaultValue: () => false,
                    "Show what you type. For devices that do not echo it back themselves");
            rootCommand.AddOption(localEchoOption);

            var timestampOption = NamedOption(
                new string[] { "--timestamp", "-ts" },
                "Prefix each line from the device with a time, abs is the clock and rel is seconds since connecting",
                "off",
                TimestampValues);
            rootCommand.AddOption(timestampOption);

            var backspaceOption = NamedOption(
                new string[] { "--backspace", "-bs" },
                "Byte sent by the Backspace key, del is 0x7F and bs is 0x08",
                "del",
                BackspaceValues);
            rootCommand.AddOption(backspaceOption);

            var newlineOption = NamedOption(
                new string[] { "--newline", "-nl" },
                "Bytes sent by the Enter key",
                "cr",
                NewlineValues);
            rootCommand.AddOption(newlineOption);

            var dbOption = new Option<int>(
                new string[] { "--data-bits", "-db" },
                getDefaultValue: () => 8,
                "Sets the standard length of data bits per byte");
            dbOption.AddCompletions("5", "6", "7", "8");
            dbOption.AddValidator(optionResult =>
            {
                if (optionResult.Tokens.Count == 0)
                    return;

                string value = optionResult.Tokens[0].Value;
                if (!int.TryParse(value, out int dataBits) || dataBits < 5 || dataBits > 8)
                    optionResult.ErrorMessage = InvalidValueMessage(optionResult, value, new[] { "5", "6", "7", "8" });
            });
            rootCommand.AddOption(dbOption);

            var parityOption = NamedOption(
                new string[] { "--parity", "-pa" },
                "Sets the parity-checking protocol",
                "None",
                ParityValues);
            rootCommand.AddOption(parityOption);

            var sbOption = NamedOption(
                new string[] { "--stop-bits", "-sb" },
                "Sets the standard number of stopbits per byte",
                "One",
                StopBitsValues);
            rootCommand.AddOption(sbOption);

            var hsOption = NamedOption(
                new string[] { "--handshake", "-hs" },
                "Specifies the control protocol used in establishing a serial port communication",
                "None",
                HandshakeValues);
            rootCommand.AddOption(hsOption);


            rootCommand.SetHandler((context)=>
                {
                    var opts = new CommandLineOptions(){
                        backspace = context.ParseResult.GetValueForOption(backspaceOption),
                        baud = context.ParseResult.GetValueForOption(baudOption),
                        dataBits = context.ParseResult.GetValueForOption(dbOption),
                        disconnectExit = context.ParseResult.GetValueForOption(disconnectExitOpen),
                        dtr = context.ParseResult.GetValueForOption(disableDTROption),
                        escapeKey = context.ParseResult.GetValueForOption(escapeKeyOption),
                        handshake = ValueOf(HandshakeValues, context.ParseResult.GetValueForOption(hsOption), Handshake.None),
                        legacyKeys = context.ParseResult.GetValueForOption(legacyKeysOption),
                        log = context.ParseResult.GetValueForOption(logOption),
                        localEcho = context.ParseResult.GetValueForOption(localEchoOption),
                        logRaw = context.ParseResult.GetValueForOption(logRawOption),
                        macros = context.ParseResult.GetValueForOption(macroOption),
                        match = context.ParseResult.GetValueForOption(matchOption),
                        sendDelay = context.ParseResult.GetValueForOption(sendDelayOption),
                        sendFile = context.ParseResult.GetValueForOption(sendFileOption),
                        sendTimeout = context.ParseResult.GetValueForOption(sendTimeoutOption),
                        sendWait = context.ParseResult.GetValueForOption(sendWaitOption),
                        newline = context.ParseResult.GetValueForOption(newlineOption),
                        noHint = context.ParseResult.GetValueForOption(noHintOption),
                        noUtf8 = context.ParseResult.GetValueForOption(noUtf8Option),
                        parity = ValueOf(ParityValues, context.ParseResult.GetValueForOption(parityOption), Parity.None),
                        port = context.ParseResult.GetValueForOption(portOption),
                        resetEsp32 = context.ParseResult.GetValueForOption(resetEsp32Option),
                        rts = context.ParseResult.GetValueForOption(disableRTSOption),
                        statusLine = context.ParseResult.GetValueForOption(statusLineOption),
                        stopBits = ValueOf(StopBitsValues, context.ParseResult.GetValueForOption(sbOption), StopBits.One),
                        timestamp = context.ParseResult.GetValueForOption(timestampOption)
                    };
                    action.Invoke(context, opts);
                }
            );
            
            return rootCommand;
        }
    }
}

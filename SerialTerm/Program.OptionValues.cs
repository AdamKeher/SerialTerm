using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO.Ports;
using System.Linq;

namespace TerminalConsole
{
    partial class Program
    {
        // The accepted spelling of each serial setting, mapped to the value it
        // stands for. One table per setting drives the completions, the
        // validator and the binding, so they cannot drift apart - the previous
        // code kept a separate list for each and compared against the wrong one.
        private static readonly Dictionary<string, Parity> ParityValues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["None"] = Parity.None,
            ["Even"] = Parity.Even,
            ["Mark"] = Parity.Mark,
            ["Odd"] = Parity.Odd,
            ["Space"] = Parity.Space,
        };

        private static readonly Dictionary<string, StopBits> StopBitsValues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["One"] = StopBits.One,
            ["OnePointFive"] = StopBits.OnePointFive,
            ["Two"] = StopBits.Two,
        };

        private static readonly Dictionary<string, Handshake> HandshakeValues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["None"] = Handshake.None,
            ["RTS"] = Handshake.RequestToSend,
            ["RTSXonXoff"] = Handshake.RequestToSendXOnXOff,
            ["XonXoff"] = Handshake.XOnXOff,
        };

        // Values are only ever looked up by name, so a set is enough
        private static readonly Dictionary<string, string> BackspaceValues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["del"] = "del",
            ["bs"] = "bs",
        };

        private static readonly Dictionary<string, string> NewlineValues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cr"] = "cr",
            ["lf"] = "lf",
            ["crlf"] = "crlf",
        };

        // An option whose value has to be one of a fixed set of names. Rejecting
        // the parse is the point: a validator that only prints lets the bad
        // value through to be silently replaced by a default further down.
        private static Option<string> NamedOption<T>(
            string[] aliases, string description, string defaultValue, IReadOnlyDictionary<string, T> values)
        {
            var option = new Option<string>(aliases, getDefaultValue: () => defaultValue, description);

            option.AddCompletions(values.Keys.ToArray());
            option.AddValidator(result =>
            {
                if (result.Tokens.Count == 0)
                    return;

                string value = result.Tokens[0].Value;
                if (!values.ContainsKey(value))
                    result.ErrorMessage = InvalidValueMessage(result, value, values.Keys);
            });

            return option;
        }

        private static string InvalidValueMessage(OptionResult result, string value, IEnumerable<string> allowed)
        {
            string name = result.Token?.Value ?? result.Option.Name;
            return $"'{value}' is not a valid value for {name}. Valid values are: {string.Join(", ", allowed)}";
        }

        private static T ValueOf<T>(IReadOnlyDictionary<string, T> values, string name, T fallback)
        {
            return name != null && values.TryGetValue(name, out T value) ? value : fallback;
        }
    }
}

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace TerminalConsole
{
    partial class Program
    {
        // A bench has the same few boards on it every day, so arguments can be
        // saved and recalled by name: @esp32 reads esp32.rsp from the profile
        // directory - %APPDATA%\SerialTerm\profiles on Windows, ~/.config
        // elsewhere. A profile named default is applied to every run without
        // being asked for.
        private const string ProfileExtension = ".rsp";
        private const string DefaultProfile = "default";

        internal static string ProfileDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SerialTerm",
                "profiles");
        }

        // Expands profiles into arguments and settles the duplicates.
        //
        // System.CommandLine expands @file itself, but it treats an option given
        // twice as an error rather than letting the later one win - so
        // `SerialTerm @bench --baud 9600` failed instead of overriding the
        // profile's baud. Expanding the files here means the duplicates can be
        // resolved before the parser ever sees them.
        internal static string[] ResolveProfiles(string[] args, Command rootCommand)
        {
            if (args == null)
                return Array.Empty<string>();

            var expanded = new List<string>();

            // defaults come first, so anything after them wins
            if (UseDefaults(args, rootCommand))
                expanded.AddRange(ReadProfile(FindProfile(DefaultProfile)));

            foreach (string argument in args)
            {
                string name = ProfileReference(argument);

                if (name == null)
                {
                    expanded.Add(argument);
                    continue;
                }

                string path = FindProfile(name) ?? (File.Exists(name) ? name : null);

                if (path == null)
                {
                    // leave it alone and let System.CommandLine report it
                    expanded.Add(argument);
                    continue;
                }

                expanded.AddRange(ReadProfile(path));
            }

            return MergeArguments(expanded, OptionKeys(rootCommand), RepeatableOptions(rootCommand));
        }

        // The last time an option is given is the one that counts, so a profile
        // or a default can be overridden by saying the option again. Options
        // that are meant to be repeated, like --macro, are left alone.
        internal static string[] MergeArguments(
            IReadOnlyList<string> arguments,
            IReadOnlyDictionary<string, string> optionKeys,
            ISet<string> repeatable)
        {
            var segments = new List<(string Key, List<string> Tokens)>();

            for (int index = 0; index < arguments.Count; index++)
            {
                string token = arguments[index];

                if (!IsOption(token))
                {
                    segments.Add((null, new List<string> { token }));
                    continue;
                }

                string key = optionKeys.TryGetValue(token, out string name) ? name : token;
                var tokens = new List<string> { token };

                // an option's value is the next token, unless that is itself an
                // option - which is how a bare switch is told from one with a
                // value, without having to know each option's arity here
                if (index + 1 < arguments.Count && !IsOption(arguments[index + 1]))
                {
                    tokens.Add(arguments[index + 1]);
                    index++;
                }

                segments.Add((repeatable.Contains(key) ? null : key, tokens));
            }

            // walk backwards so the last occurrence is the one kept
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var keep = new bool[segments.Count];

            for (int index = segments.Count - 1; index >= 0; index--)
                keep[index] = segments[index].Key == null || seen.Add(segments[index].Key);

            var merged = new List<string>();

            for (int index = 0; index < segments.Count; index++)
                if (keep[index])
                    merged.AddRange(segments[index].Tokens);

            return merged.ToArray();
        }

        private static bool IsOption(string token)
        {
            return token != null && token.Length > 1 && token[0] == '-';
        }

        // Every alias mapped to the option it belongs to, so --status-line and
        // -sl are recognised as the same setting.
        internal static Dictionary<string, string> OptionKeys(Command command)
        {
            var keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (command == null)
                return keys;

            foreach (Option option in command.Options)
                foreach (string alias in option.Aliases)
                    keys[alias] = option.Name;

            return keys;
        }

        internal static HashSet<string> RepeatableOptions(Command command)
        {
            var repeatable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (command == null)
                return repeatable;

            foreach (Option option in command.Options)
                if (option.Arity.MaximumNumberOfValues > 1)
                    repeatable.Add(option.Name);

            return repeatable;
        }

        // Defaults are for the terminal, not for `list` or `profiles`, and
        // --no-defaults turns them off for one run.
        private static bool UseDefaults(string[] args, Command rootCommand)
        {
            foreach (string argument in args)
                if (argument.Equals("--no-defaults", StringComparison.OrdinalIgnoreCase)
                    || argument.Equals("-nd", StringComparison.OrdinalIgnoreCase))
                    return false;

            if (args.Length > 0 && rootCommand != null)
                foreach (Command child in rootCommand.Subcommands)
                    if (args[0].Equals(child.Name, StringComparison.OrdinalIgnoreCase))
                        return false;

            return true;
        }

        // "@esp32" -> "esp32". Anything else is not a profile reference.
        private static string ProfileReference(string argument)
        {
            return argument != null && argument.Length > 1 && argument[0] == '@'
                ? argument.Substring(1)
                : null;
        }

        // One argument per line. Blank lines and # comments are skipped, so a
        // profile can say why it is the way it is.
        internal static IEnumerable<string> ReadProfile(string path)
        {
            if (path == null)
                yield break;

            string[] lines;

            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                yield break;
            }

            foreach (string line in lines)
            {
                string argument = line.Trim();

                if (argument.Length > 0 && argument[0] != '#')
                    yield return argument;
            }
        }

        private static string FindProfile(string name)
        {
            try
            {
                string directory = ProfileDirectory();

                foreach (string candidate in new[] { name, name + ProfileExtension })
                {
                    string path = Path.Combine(directory, candidate);

                    if (File.Exists(path))
                        return path;
                }
            }
            catch (Exception)
            {
                // an unusable profile directory just means no profiles
            }

            return null;
        }

        internal static IEnumerable<string> ProfileNames()
        {
            string directory = ProfileDirectory();

            if (!Directory.Exists(directory))
                yield break;

            foreach (string path in Directory.GetFiles(directory, "*" + ProfileExtension))
                yield return Path.GetFileNameWithoutExtension(path);
        }

        private static void ProfilesCommandHandler()
        {
            string directory = ProfileDirectory();

            SayLine("Profiles");
            SayLine("--------");
            SayLine(directory);
            SayLine();

            var rows = new List<string[]>();

            foreach (string name in ProfileNames())
            {
                string path = Path.Combine(directory, name + ProfileExtension);
                string arguments = string.Join(" ", ReadProfile(path));

                bool isDefault = name.Equals(DefaultProfile, StringComparison.OrdinalIgnoreCase);

                rows.Add(new[]
                {
                    isDefault ? name : "@" + name,
                    isDefault ? "applied to every run" : string.Empty,
                    arguments,
                });
            }

            if (rows.Count == 0)
            {
                SayLine("No profiles yet.");
                SayLine();
                SayLine($"  {Path.Combine(directory, DefaultProfile + ProfileExtension)}");
                SayLine("    one argument per line, applied to every run");
                SayLine();
                SayLine($"  {Path.Combine(directory, "esp32" + ProfileExtension)}");
                SayLine("    the same, but only when asked for: SerialTerm @esp32");
                return;
            }

            WriteTable(new[] { "Profile", "When", "Arguments" }, rows);
        }
    }
}

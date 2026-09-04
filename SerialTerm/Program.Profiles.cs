using System;
using System.Collections.Generic;
using System.IO;

namespace TerminalConsole
{
    partial class Program
    {
        // System.CommandLine already expands @file into arguments. That covers a
        // response file sitting next to the project, but a bench has the same
        // few boards on it every day, so @name also looks in a profile
        // directory - %APPDATA%\SerialTerm\profiles on Windows, ~/.config
        // elsewhere.
        private const string ProfileExtension = ".rsp";

        internal static string ProfileDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SerialTerm",
                "profiles");
        }

        // A local file wins, so a project can keep its own settings in the
        // repository and they are not shadowed by a profile of the same name.
        internal static string[] ResolveProfiles(string[] args)
        {
            if (args == null)
                return Array.Empty<string>();

            var resolved = new string[args.Length];

            for (int index = 0; index < args.Length; index++)
            {
                resolved[index] = args[index];

                string name = ProfileName(args[index]);
                if (name == null)
                    continue;

                string path = FindProfile(name);
                if (path != null)
                    resolved[index] = "@" + path;
            }

            return resolved;
        }

        // "@esp32" -> "esp32", and null for anything that is not a reference to
        // a profile we should resolve
        private static string ProfileName(string argument)
        {
            if (argument == null || argument.Length < 2 || argument[0] != '@')
                return null;

            string name = argument.Substring(1);

            // already a usable path, leave it for System.CommandLine
            if (File.Exists(name))
                return null;

            return name;
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
                string arguments;

                try
                {
                    arguments = string.Join(" ", File.ReadAllLines(path)).Trim();
                }
                catch (IOException e)
                {
                    arguments = $"(unreadable: {e.Message})";
                }

                rows.Add(new[] { "@" + name, arguments });
            }

            if (rows.Count == 0)
            {
                SayLine("No profiles yet. Create one with:");
                SayLine($"  {Path.Combine(directory, "esp32" + ProfileExtension)}");
                SayLine("holding one argument per line, then run: SerialTerm @esp32");
                return;
            }

            WriteTable(new[] { "Profile", "Arguments" }, rows);
        }
    }
}

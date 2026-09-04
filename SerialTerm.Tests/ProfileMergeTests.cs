using System.Collections.Generic;
using TerminalConsole;
using Xunit;

namespace SerialTerm.Tests
{
    // System.CommandLine treats an option given twice as an error rather than
    // letting the later one win, so `SerialTerm @bench --baud 9600` used to fail
    // instead of overriding the profile. Duplicates are settled here, before the
    // parser sees them.
    public class ProfileMergeTests
    {
        // --status-line and -sl are the same setting; --macro is meant to repeat
        private static readonly Dictionary<string, string> Keys = new()
        {
            ["--baud"] = "baud",
            ["-b"] = "baud",
            ["--status-line"] = "status-line",
            ["-sl"] = "status-line",
            ["--port"] = "port",
            ["-P"] = "port",
            ["--macro"] = "macro",
            ["-m"] = "macro",
            ["--timestamp"] = "timestamp",
            ["-ts"] = "timestamp",
        };

        private static readonly HashSet<string> Repeatable = new() { "macro" };

        private static string[] Merge(params string[] arguments)
        {
            return Program.MergeArguments(arguments, Keys, Repeatable);
        }

        [Fact]
        public void LeavesArgumentsWithoutDuplicatesAlone()
        {
            Assert.Equal(
                new[] { "--port", "COM3", "--baud", "9600" },
                Merge("--port", "COM3", "--baud", "9600"));
        }

        [Fact]
        public void LastValueWins()
        {
            // the profile's baud comes first, the explicit one overrides it
            Assert.Equal(
                new[] { "--baud", "9600" },
                Merge("--baud", "115200", "--baud", "9600"));
        }

        [Fact]
        public void RecognisesAnAliasAsTheSameOption()
        {
            Assert.Equal(
                new[] { "-b", "9600" },
                Merge("--baud", "115200", "-b", "9600"));
        }

        [Fact]
        public void RecognisesTheLongFormOverridingTheShort()
        {
            Assert.Equal(
                new[] { "--baud", "9600" },
                Merge("-b", "115200", "--baud", "9600"));
        }

        [Fact]
        public void KeepsSwitchesThatTakeNoValue()
        {
            Assert.Equal(
                new[] { "--status-line", "--port", "COM3" },
                Merge("--status-line", "--port", "COM3"));
        }

        [Fact]
        public void LetsASwitchBeTurnedOffExplicitly()
        {
            // the default profile switched it on, this run switches it back off
            Assert.Equal(
                new[] { "--status-line", "false" },
                Merge("--status-line", "--status-line", "false"));
        }

        [Fact]
        public void KeepsRepeatableOptions()
        {
            Assert.Equal(
                new[] { "--macro", "1=a", "--macro", "2=b", "--macro", "3=c" },
                Merge("--macro", "1=a", "--macro", "2=b", "--macro", "3=c"));
        }

        [Fact]
        public void KeepsRepeatableOptionsAcrossAliases()
        {
            Assert.Equal(
                new[] { "--macro", "1=a", "-m", "2=b" },
                Merge("--macro", "1=a", "-m", "2=b"));
        }

        [Fact]
        public void KeepsPositionalArguments()
        {
            Assert.Equal(
                new[] { "list", "--probe" },
                Merge("list", "--probe"));
        }

        [Fact]
        public void OverridesOnlyTheOptionRepeated()
        {
            // a whole profile, with one setting changed for this run
            Assert.Equal(
                new[] { "--port", "COM3", "--status-line", "--timestamp", "rel", "--baud", "9600" },
                Merge("--port", "COM3", "--baud", "115200", "--status-line", "--timestamp", "rel",
                      "--baud", "9600"));
        }

        [Fact]
        public void HandlesThreeLayersOfOverride()
        {
            // default profile, then a named profile, then the command line
            Assert.Equal(
                new[] { "--baud", "19200" },
                Merge("--baud", "9600", "--baud", "115200", "--baud", "19200"));
        }

        [Fact]
        public void HandlesAnEmptyArgumentList()
        {
            Assert.Empty(Merge());
        }

        [Fact]
        public void LeavesUnknownOptionsForTheParserToReject()
        {
            Assert.Equal(
                new[] { "--nonsense", "value" },
                Merge("--nonsense", "value"));
        }

        [Fact]
        public void TreatsAnUnknownOptionRepeatedAsTheSameOption()
        {
            Assert.Equal(
                new[] { "--nonsense", "second" },
                Merge("--nonsense", "first", "--nonsense", "second"));
        }
    }
}

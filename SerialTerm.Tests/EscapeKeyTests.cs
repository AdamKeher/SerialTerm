using TerminalConsole;
using Xunit;

namespace SerialTerm.Tests
{
    public class EscapeKeyTests
    {
        [Theory]
        [InlineData("^A", 0x01)]
        [InlineData("^]", 0x1D)]
        [InlineData("Ctrl+A", 0x01)]
        [InlineData("ctrl+q", 0x11)]
        [InlineData("0x1D", 0x1D)]
        [InlineData("0x01", 0x01)]
        [InlineData("A", 0x01)]
        [InlineData("]", 0x1D)]
        public void ParsesEveryAcceptedForm(string input, int expected)
        {
            Assert.Equal((char)expected, Program.ParseEscapeKey(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not a key")]
        public void FallsBackToCtrlAOnNonsense(string input)
        {
            Assert.Equal((char)0x01, Program.ParseEscapeKey(input));
        }

        [Fact]
        public void TrimsSurroundingSpace()
        {
            Assert.Equal((char)0x1D, Program.ParseEscapeKey("  ^]  "));
        }

        [Theory]
        [InlineData(0x01, "Ctrl+A")]
        [InlineData(0x1D, "Ctrl+]")]
        [InlineData(0x11, "Ctrl+Q")]
        public void NamesControlKeysReadably(int key, string expected)
        {
            Program._escapeKey = (char)key;
            Assert.Equal(expected, Program.EscapeKeyName());
        }
    }
}

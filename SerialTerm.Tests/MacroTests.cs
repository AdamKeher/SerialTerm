using TerminalConsole;
using Xunit;

namespace SerialTerm.Tests
{
    public class MacroTests
    {
        [Theory]
        [InlineData(@"reboot", new byte[] { 114, 101, 98, 111, 111, 116 })]
        [InlineData(@"a\r", new byte[] { 97, 0x0D })]
        [InlineData(@"a\n", new byte[] { 97, 0x0A })]
        [InlineData(@"a\t", new byte[] { 97, 0x09 })]
        [InlineData(@"a\0", new byte[] { 97, 0x00 })]
        [InlineData(@"\e", new byte[] { 0x1B })]
        [InlineData(@"a\b", new byte[] { 97, 0x5C, 98 })]
        [InlineData(@"\x41\x42", new byte[] { 0x41, 0x42 })]
        [InlineData(@"\xff", new byte[] { 0xFF })]
        [InlineData(@"ls\r\n", new byte[] { 108, 115, 0x0D, 0x0A })]
        public void UnderstandsTheUsualEscapes(string text, byte[] expected)
        {
            Assert.Equal(expected, Program.Unescape(text));
        }

        [Theory]
        [InlineData(@"ab\", new byte[] { 97, 98, 0x5C })]
        [InlineData(@"a\qb", new byte[] { 97, 0x5C, 113, 98 })]
        [InlineData(@"\xZZ", new byte[] { 120, 90, 90 })]
        [InlineData(@"a\x4", new byte[] { 97, 120, 52 })]
        public void KeepsWhatItCannotInterpret(string text, byte[] expected)
        {
            Assert.Equal(expected, Program.Unescape(text));
        }

        [Fact]
        public void HandlesAnEmptyMacro()
        {
            Assert.Empty(Program.Unescape(""));
        }

        [Fact]
        public void SendsUnicodeAsUtf8()
        {
            Assert.Equal(new byte[] { 0xC3, 0xA9 }, Program.Unescape("é"));
        }

        [Fact]
        public void HandlesARealisticBootloaderMacro()
        {
            // Ctrl+C to interrupt, then a command
            Assert.Equal(new byte[] { 0x03, 108, 115, 0x0D }, Program.Unescape(@"\x03ls\r"));
        }
    }
}

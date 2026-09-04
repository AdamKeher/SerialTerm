using System.Text;
using TerminalConsole;
using Xunit;

namespace SerialTerm.Tests
{
    // A log full of cursor movement is unreadable and ungreppable, so the
    // sequences come out. The filter is a state machine because a sequence can
    // be split across two reads from the port, which is the case that breaks
    // naive implementations.
    public class AnsiFilterTests
    {
        private const char Esc = (char)0x1B;
        private const char Bel = (char)0x07;
        private const char Backslash = (char)0x5C;

        private static string Filter(string input)
        {
            byte[] source = Encoding.ASCII.GetBytes(input);
            byte[] destination = new byte[source.Length];
            int written = Program.StripAnsi(source, source.Length, destination);
            return Encoding.ASCII.GetString(destination, 0, written);
        }

        private static string FilterFresh(string input)
        {
            Program._ansiState = Program.AnsiState.Text;
            return Filter(input);
        }

        [Fact]
        public void LeavesPlainTextAlone()
        {
            Assert.Equal("hello world\r\n", FilterFresh("hello world\r\n"));
        }

        [Fact]
        public void RemovesColour()
        {
            Assert.Equal("RED", FilterFresh($"{Esc}[31mRED{Esc}[0m"));
        }

        [Fact]
        public void RemovesCursorMovement()
        {
            Assert.Equal("boot", FilterFresh($"{Esc}[2J{Esc}[1;1Hboot"));
        }

        [Fact]
        public void RemovesMultiParameterSequences()
        {
            Assert.Equal("ab", FilterFresh($"a{Esc}[1;5;38;2;255;0;0mb"));
        }

        [Fact]
        public void RemovesOscTitleEndedByBell()
        {
            Assert.Equal("x", FilterFresh($"{Esc}]0;my title{Bel}x"));
        }

        [Fact]
        public void RemovesOscTitleEndedByStringTerminator()
        {
            Assert.Equal("x", FilterFresh($"{Esc}]0;my title{Esc}{Backslash}x"));
        }

        [Fact]
        public void RemovesTwoByteSequences()
        {
            Assert.Equal("abc", FilterFresh($"a{Esc}=b{Esc}>c"));
        }

        [Fact]
        public void HandlesAFullScreenRedraw()
        {
            Assert.Equal("line1\r\n~\r\n",
                FilterFresh($"{Esc}[?25l{Esc}[H{Esc}[2Kline1\r\n{Esc}[K~\r\n{Esc}[?25h"));
        }

        [Fact]
        public void CarriesStateAcrossReads()
        {
            // the sequence is split in the middle, as the port would deliver it
            Assert.Equal("AA", FilterFresh($"AA{Esc}[3"));
            Assert.Equal("BB", Filter("1mBB"));
        }

        [Fact]
        public void CarriesStateWhenOnlyEscapeArrives()
        {
            Assert.Equal("x", FilterFresh($"x{Esc}"));
            Assert.Equal("Y", Filter("[0mY"));
        }

        [Fact]
        public void KeepsHighBytesIntact()
        {
            Program._ansiState = Program.AnsiState.Text;
            byte[] source = { 0xC3, 0xA9, 0x21 };
            byte[] destination = new byte[3];
            int written = Program.StripAnsi(source, 3, destination);

            Assert.Equal(3, written);
            Assert.Equal(new byte[] { 0xC3, 0xA9, 0x21 }, destination);
        }
    }
}

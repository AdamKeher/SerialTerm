using System;
using System.Text;
using TerminalConsole;
using Xunit;

namespace SerialTerm.Tests
{
    // Serial data arrives in whatever chunks the driver hands over, so a line
    // routinely spans two reads. The line position has to carry across them, or
    // every read starts with a stamp in the middle of a line.
    public class TimestampTests
    {
        private readonly StringBuilder _sink = new StringBuilder();
        private bool _atLineStart = true;

        private void Feed(string text, params int[] chunks)
        {
            byte[] all = Encoding.ASCII.GetBytes(text);
            int position = 0;
            int chunk = 0;

            while (position < all.Length)
            {
                int size = chunks.Length == 0
                    ? all.Length - position
                    : Math.Min(chunks[chunk++ % chunks.Length], all.Length - position);

                byte[] slice = new byte[size];
                Array.Copy(all, position, slice, 0, size);

                Program.WriteTimestamped(slice, size, ref _atLineStart,
                    (buffer, offset, count) => _sink.Append(Encoding.ASCII.GetString(buffer, offset, count)),
                    stamp => _sink.Append("<TS>"));

                position += size;
            }
        }

        private string Result => _sink.ToString();

        [Fact]
        public void StampsASingleLine()
        {
            Feed("boot ok\n");
            Assert.Equal("<TS>boot ok\n", Result);
        }

        [Fact]
        public void StampsEveryLine()
        {
            Feed("a\nb\nc\n");
            Assert.Equal("<TS>a\n<TS>b\n<TS>c\n", Result);
        }

        [Fact]
        public void HandlesCrLfEndings()
        {
            Feed("a\r\nb\r\n");
            Assert.Equal("<TS>a\r\n<TS>b\r\n", Result);
        }

        [Fact]
        public void StampsBlankLines()
        {
            Feed("a\n\nb\n");
            Assert.Equal("<TS>a\n<TS>\n<TS>b\n", Result);
        }

        [Fact]
        public void StampsATrailingPartialLine()
        {
            Feed("a\npartial");
            Assert.Equal("<TS>a\n<TS>partial", Result);
        }

        [Fact]
        public void DoesNotStampMidLineWhenSplitAcrossReads()
        {
            Feed("hello world\n", 5);
            Assert.Equal("<TS>hello world\n", Result);
        }

        [Fact]
        public void DoesNotStampMidLineOneByteAtATime()
        {
            Feed("ab\ncd\n", 1);
            Assert.Equal("<TS>ab\n<TS>cd\n", Result);
        }

        [Fact]
        public void HandlesANewlineLandingOnAChunkBoundary()
        {
            Feed("abc\ndef\n", 4);
            Assert.Equal("<TS>abc\n<TS>def\n", Result);
        }

        [Fact]
        public void DoesNotStampAReplyToAPromptOnTheSameLine()
        {
            Feed("$ ");
            Feed("ls\n");
            Assert.Equal("<TS>$ ls\n", Result);
        }

        [Theory]
        [InlineData("off", false)]
        [InlineData("abs", true)]
        [InlineData("rel", true)]
        [InlineData("nonsense", false)]
        public void ConfiguresFromTheOptionValue(string mode, bool enabled)
        {
            Program.ConfigureTimestamps(mode);
            Assert.Equal(enabled, Program._timestamps != Program.TimestampMode.Off);
        }

        [Fact]
        public void RelativeStampIsBracketed()
        {
            Program.ConfigureTimestamps("rel");
            string stamp = Program.TimestampPrefix();

            Assert.StartsWith("[", stamp);
            Assert.EndsWith("] ", stamp);
        }
    }
}

using System.IO;
using System.Text;
using TerminalConsole;
using Xunit;

namespace SerialTerm.Tests
{
    // Each read from the port ends its line rather than holding bytes back
    // until sixteen arrive, so a short reply appears at once and burst
    // boundaries stay visible - which for a framed protocol is usually where the
    // frame boundaries are.
    public class HexViewTests
    {
        private static string Render(params byte[][] bursts)
        {
            var sink = new MemoryStream();
            Program._standardOutput = sink;
            Program._hexOffset = 0;
            Program._hexCount = 0;

            foreach (byte[] burst in bursts)
                Program.RenderHex(burst, burst.Length);

            return Encoding.ASCII.GetString(sink.ToArray());
        }

        [Fact]
        public void RendersAFullLine()
        {
            Assert.Equal(
                "00000000  48 65 6c 6c 6f 20 77 6f  72 6c 64 20 31 32 33 34 |Hello world 1234|\r\n",
                Render(Encoding.ASCII.GetBytes("Hello world 1234")));
        }

        [Fact]
        public void FlushesAShortBurstImmediately()
        {
            Assert.Equal(
                "00000000  4f 4b 0d 0a                                      |OK..|\r\n",
                Render(Encoding.ASCII.GetBytes("OK\r\n")));
        }

        [Fact]
        public void KeepsBurstsOnSeparateLinesWithRunningOffsets()
        {
            string rendered = Render(
                new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0xC4, 0x0B },
                new byte[] { 0x01, 0x03, 0x04, 0x00, 0x0A, 0x00, 0x14, 0xDA, 0x31 });

            string[] lines = rendered.TrimEnd().Split("\r\n");

            Assert.Equal(2, lines.Length);
            Assert.StartsWith("00000000  01 03 00 00 00 02 c4 0b", lines[0]);
            Assert.StartsWith("00000008  01 03 04 00 0a 00 14 da  31", lines[1]);
        }

        [Fact]
        public void WrapsEverySixteenBytes()
        {
            byte[] buffer = new byte[35];
            for (int index = 0; index < buffer.Length; index++)
                buffer[index] = (byte)index;

            string[] lines = Render(buffer).TrimEnd().Split("\r\n");

            Assert.Equal(3, lines.Length);
            Assert.StartsWith("00000000", lines[0]);
            Assert.StartsWith("00000010", lines[1]);
            Assert.StartsWith("00000020", lines[2]);
        }

        [Fact]
        public void ShowsNonPrintableBytesAsDots()
        {
            string rendered = Render(new byte[] { 0x00, 0x1B, 0x7F, 0x80, 0xFF, 0x41, 0x42 });
            Assert.Contains("|.....AB|", rendered);
        }

        [Fact]
        public void AlignsTheAsciiColumnWhateverTheLineLength()
        {
            int full = Render(Encoding.ASCII.GetBytes("0123456789abcdef")).IndexOf('|');
            int partial = Render(Encoding.ASCII.GetBytes("short")).IndexOf('|');

            Assert.Equal(full, partial);
        }
    }
}

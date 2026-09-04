using System.IO;
using System.Text;
using TerminalConsole;
using Xunit;

namespace SerialTerm.Tests
{
    // Issue #3: a device reporting "44°C" showed up as "44┬░C". The bytes were
    // always right - C2 B0 is a correct UTF-8 degree sign - but the console was
    // decoding them in its OEM code page, where those two bytes are two glyphs.
    //
    // The fix is to tell the console the stream is UTF-8, which is console state
    // and cannot be asserted here. What can be pinned down is the half that
    // would break if someone reached for the other fix: transcoding the bytes
    // on the way out. That would mangle every device that is not UTF-8, so
    // these tests exist to say the stream is passed through untouched.
    public class DeviceOutputTests
    {
        private static MemoryStream Plain()
        {
            var sink = new MemoryStream();

            Program._standardOutput = sink;
            Program._hexView = false;
            Program._frozen = false;
            Program.ConfigureTimestamps("off");

            return sink;
        }

        [Fact]
        public void PassesAUtf8DegreeSignThroughUnchanged()
        {
            MemoryStream sink = Plain();

            // exactly what the device in issue #3 sends
            byte[] sent = Encoding.UTF8.GetBytes("CPU temp:     44°C");
            Program.RenderDeviceBytes(sent, sent.Length);

            Assert.Equal(sent, sink.ToArray());
        }

        [Fact]
        public void KeepsTheTwoByteSequenceIntact()
        {
            MemoryStream sink = Plain();

            byte[] sent = { 0x34, 0x34, 0xC2, 0xB0, 0x43 };
            Program.RenderDeviceBytes(sent, sent.Length);

            Assert.Equal(sent, sink.ToArray());
        }

        [Theory]
        [InlineData("é")]
        [InlineData("°")]
        [InlineData("µ")]
        [InlineData("Ω")]
        [InlineData("→")]
        [InlineData("█")]
        [InlineData("日本語")]
        public void PassesEveryMultiByteCharacterThrough(string text)
        {
            MemoryStream sink = Plain();

            byte[] sent = Encoding.UTF8.GetBytes(text);
            Program.RenderDeviceBytes(sent, sent.Length);

            Assert.Equal(sent, sink.ToArray());
        }

        [Fact]
        public void PassesBytesThatAreNotValidUtf8Through()
        {
            MemoryStream sink = Plain();

            // a device sending Latin-1 or binary must not be rewritten either
            byte[] sent = { 0xFF, 0xFE, 0x80, 0x00, 0x41 };
            Program.RenderDeviceBytes(sent, sent.Length);

            Assert.Equal(sent, sink.ToArray());
        }

        [Fact]
        public void PassesAMultiByteCharacterSplitAcrossReads()
        {
            MemoryStream sink = Plain();

            // the port can deliver C2 and B0 in separate reads
            Program.RenderDeviceBytes(new byte[] { 0x34, 0xC2 }, 2);
            Program.RenderDeviceBytes(new byte[] { 0xB0, 0x43 }, 2);

            Assert.Equal(new byte[] { 0x34, 0xC2, 0xB0, 0x43 }, sink.ToArray());
        }
    }
}

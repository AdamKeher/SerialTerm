using System.IO;
using System.Text;
using TerminalConsole;
using Xunit;

namespace SerialTerm.Tests
{
    // A device in a reboot loop would grow the freeze buffer without limit, so
    // it is capped and the oldest bytes go - the reason to freeze is almost
    // always to read something that just happened.
    public class FreezeTests
    {
        private static void Start()
        {
            Program._freezeBuffer = new MemoryStream();
            Program._freezeDropped = 0;
        }

        private static void Hold(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            Program.HoldWhileFrozen(bytes, bytes.Length);
        }

        private static string Held()
        {
            return Encoding.ASCII.GetString(Program._freezeBuffer.ToArray());
        }

        [Fact]
        public void HoldsBytesInOrder()
        {
            Start();
            Hold("stack trace line one\r\n");
            Hold("line two\r\n");

            Assert.Equal("stack trace line one\r\nline two\r\n", Held());
            Assert.Equal(0, Program._freezeDropped);
        }

        [Fact]
        public void KeepsEverythingUpToTheCap()
        {
            Start();
            Hold(new string('x', Program.FreezeBufferLimit));

            Assert.Equal(Program.FreezeBufferLimit, Program._freezeBuffer.Length);
            Assert.Equal(0, Program._freezeDropped);
        }

        [Fact]
        public void DropsTheOldestBytesPastTheCap()
        {
            Start();
            Hold(new string('x', Program.FreezeBufferLimit));
            Hold("NEWEST");

            byte[] held = Program._freezeBuffer.ToArray();

            Assert.Equal(Program.FreezeBufferLimit, held.Length);
            Assert.Equal("NEWEST", Encoding.ASCII.GetString(held, held.Length - 6, 6));
            Assert.Equal(6, Program._freezeDropped);
        }

        [Fact]
        public void AccumulatesTheDroppedCountAcrossOverflows()
        {
            Start();
            Hold(new string('x', Program.FreezeBufferLimit));
            Hold("NEWEST");
            Hold("MORE");

            byte[] held = Program._freezeBuffer.ToArray();

            Assert.Equal(Program.FreezeBufferLimit, held.Length);
            Assert.Equal(10, Program._freezeDropped);
            Assert.Equal("MORE", Encoding.ASCII.GetString(held, held.Length - 4, 4));
        }
    }
}

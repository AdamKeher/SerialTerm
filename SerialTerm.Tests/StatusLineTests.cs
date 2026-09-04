using System.IO;
using System.IO.Ports;
using System.Text;
using TerminalConsole;
using Xunit;

namespace SerialTerm.Tests
{
    // The escapes are asserted byte for byte because C#'s \x takes one to four
    // hex digits: "\x1b7" is the single character U+01B7, not ESC followed by 7,
    // which silently turns DECSC and DECRC into garbage and leaves the cursor
    // parked on the status row.
    public class StatusLineTests
    {
        private const char Esc = (char)0x1B;

        private static MemoryStream Prepare(int rows = 24, int columns = 60)
        {
            var sink = new MemoryStream();

            Program._standardOutput = sink;
            Program._statusRows = rows;
            Program._statusColumns = columns;

            return sink;
        }

        private static string Painted(MemoryStream sink)
        {
            return Encoding.ASCII.GetString(sink.ToArray());
        }

        private static SerialPort Port()
        {
            var port = new SerialPort
            {
                PortName = "COM9",
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
            };

            Program._serialPort = port;
            return port;
        }

        [Fact]
        public void SavesAndRestoresTheCursorAroundThePaint()
        {
            var sink = Prepare();
            Program.PaintBottomLine("TEST");

            string painted = Painted(sink);

            Assert.StartsWith($"{Esc}7", painted);
            Assert.EndsWith($"{Esc}8", painted);
        }

        [Fact]
        public void AddressesTheReservedRowInReverseVideo()
        {
            var sink = Prepare();
            Program.PaintBottomLine("TEST");

            string painted = Painted(sink);

            Assert.Contains($"{Esc}[24;1H", painted);
            Assert.Contains($"{Esc}[7m", painted);
            Assert.Contains($"{Esc}[0m", painted);
        }

        [Fact]
        public void PadsToExactlyTheWindowWidth()
        {
            var sink = Prepare(columns: 60);
            Program.PaintBottomLine("TEST");

            string visible = Painted(sink)
                .Replace($"{Esc}7", "").Replace($"{Esc}[24;1H", "")
                .Replace($"{Esc}[7m", "").Replace($"{Esc}[0m", "").Replace($"{Esc}8", "");

            Assert.Equal(60, visible.Length);
        }

        [Fact]
        public void TruncatesTextWiderThanTheWindow()
        {
            var sink = Prepare(columns: 20);
            Program.PaintBottomLine(new string('x', 200));

            string visible = Painted(sink)
                .Replace($"{Esc}7", "").Replace($"{Esc}[24;1H", "")
                .Replace($"{Esc}[7m", "").Replace($"{Esc}[0m", "").Replace($"{Esc}8", "");

            Assert.Equal(20, visible.Length);
        }

        [Fact]
        public void ReportsThePortAndItsSettings()
        {
            Prepare();
            Port();

            string status = Program.StatusText(100);

            Assert.Contains("COM9", status);
            Assert.Contains("115200", status);
            Assert.Contains("8N1", status);
            Assert.Contains("DTR", status);
            Assert.Contains("RTS", status);
        }

        [Fact]
        public void ReportsAClosedPortAsDisconnected()
        {
            Prepare();
            Port();

            Assert.Contains("DISCONNECTED", Program.StatusText(100));
        }

        [Fact]
        public void FlagsTheModesThatAreOn()
        {
            Prepare();
            Port();

            Program._hexView = true;
            Program._frozen = true;
            Program._localEcho = true;

            string status = Program.StatusText(100);

            Assert.Contains("HEX", status);
            Assert.Contains("FROZEN", status);
            Assert.Contains("ECHO", status);

            Program._hexView = false;
            Program._frozen = false;
            Program._localEcho = false;
        }

        [Theory]
        [InlineData(8, Parity.None, StopBits.One, "8N1")]
        [InlineData(7, Parity.Even, StopBits.Two, "7E2")]
        [InlineData(8, Parity.Odd, StopBits.OnePointFive, "8O1.5")]
        [InlineData(5, Parity.Mark, StopBits.One, "5M1")]
        [InlineData(6, Parity.Space, StopBits.One, "6S1")]
        public void SummarisesTheFraming(int dataBits, Parity parity, StopBits stopBits, string expected)
        {
            Prepare();
            SerialPort port = Port();

            port.DataBits = dataBits;
            port.Parity = parity;
            port.StopBits = stopBits;

            Assert.Equal(expected, Program.Framing());
        }
    }
}

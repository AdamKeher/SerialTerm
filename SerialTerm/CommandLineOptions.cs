using System.IO.Ports;

namespace TerminalConsole
{
    public class CommandLineOptions
    {
        public string port { get; set; }
        public int baud { get; set; }
        public int dataBits { get; set; }
        public Parity parity { get; set; }
        public StopBits stopBits { get; set; }
        public Handshake handshake { get; set; }
        public bool disconnectExit { get; set; }
        public bool resetEsp32 { get; set; }
        public bool dtr { get; set; }
        public bool rts { get; set; }
        public string escapeKey { get; set; }
        public bool legacyKeys { get; set; }
        public bool noHint { get; set; }
        public string backspace { get; set; }
        public string newline { get; set; }
    }
}
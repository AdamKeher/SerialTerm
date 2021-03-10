namespace TerminalConsole
{
    public class CommandLineOptions
    {
        public string port { get; set; }
        public int baud { get; set; }
        public int dataBits { get; set; }
        public string parity { get; set; }
        public string stopBits { get; set; }
        public string handshake { get; set; }
        public bool disconnectExit { get; set; }
    }
}
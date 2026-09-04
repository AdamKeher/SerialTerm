namespace TerminalConsole
{
    partial class Program
    {
        // A script driving SerialTerm needs to tell "the user quit" from "the
        // port never opened". System.CommandLine already returns 1 for a parse
        // error, so the terminal's own outcomes start at 2.
        private const int ExitOk = 0;
        private const int ExitNoPort = 2;
        private const int ExitDisconnected = 3;
        private const int ExitBadSettings = 4;
    }
}

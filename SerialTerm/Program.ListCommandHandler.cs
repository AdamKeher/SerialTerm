using System;
using System.CommandLine.Invocation;
using System.IO.Ports;
using System.Threading;

namespace TerminalConsole
{
    partial class Program
    {
        static void ListCommmandHandler(bool probe)
        {
            SayLine("Serial Ports");
            SayLine("------------");
            DisplayPorts(probe);
        }
    }
}

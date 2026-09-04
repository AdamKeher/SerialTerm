using System;
using System.IO;
using System.Threading;

namespace TerminalConsole
{
    partial class Program
    {
        // A break is a run of zero bits longer than a character frame, which is
        // why it cannot be expressed as a byte and needs the driver's help.
        // 250ms clears the usual thresholds comfortably.
        private const int BreakDuration = 250;

        // Ctrl+A b. Interrupts U-Boot, reaches Linux SysRq, and drops some
        // bootloaders into command mode - none of which any sequence of bytes
        // can do.
        private static void SendBreak()
        {
            if (!_serialPort.IsOpen)
            {
                SayLine("\r\nNot connected.");
                return;
            }

            Say($"\r\nSending break ({BreakDuration}ms) ... ");

            try
            {
                _serialPort.BreakState = true;
                Thread.Sleep(BreakDuration);
                _serialPort.BreakState = false;

                SayLine("done");
            }
            catch (Exception e) when (e is InvalidOperationException || e is IOException)
            {
                SayLine($"failed: {e.Message}");
            }
        }
    }
}

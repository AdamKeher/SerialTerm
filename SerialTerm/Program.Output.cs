using System;

namespace TerminalConsole
{
    partial class Program
    {
        // Everything the device sends passes through here, so the sinks that
        // want a copy - the log file, and later the alternate views - all see
        // the same bytes in the same order. Held under _consoleLock, the same
        // lock SerialTerm's own output takes, so nothing interleaves.
        private static void WriteDeviceBytes(byte[] buffer, int count)
        {
            if (count <= 0)
                return;

            lock (_consoleLock)
            {
                // the log records what the device sent, whatever the console
                // is currently doing with it
                LogBytes(buffer, count);

                bool hint = _hintVisible;
                if (hint) HideHint();

                if (_hexView)
                    RenderHex(buffer, count);
                else if (TimestampsEnabled)
                    WriteTimestamped(buffer, count, ref _consoleAtLineStart,
                        WriteRaw, text => WriteRaw(Ascii(text), 0, text.Length));
                else
                    WriteRaw(buffer, 0, count);

                if (hint) ShowHint();
            }
        }

        // Device output goes to the console as bytes. Decoding it as text first
        // would corrupt anything outside plain ASCII and is not needed - the
        // console interprets the escape sequences itself.
        private static void WriteRaw(byte[] buffer, int offset, int count)
        {
            _standardOutput ??= Console.OpenStandardOutput();
            _standardOutput.Write(buffer, offset, count);
            _standardOutput.Flush();
        }
    }
}

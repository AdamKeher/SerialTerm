using System;
using System.Text;

namespace TerminalConsole
{
    partial class Program
    {
        internal enum TimestampMode { Off, Absolute, Relative }

        internal static TimestampMode _timestamps = TimestampMode.Off;
        private static DateTime _connectedAt = DateTime.UtcNow;

        // the console and the log are separate streams and can be mid line at
        // different points, so each tracks its own position
        internal static bool _consoleAtLineStart = true;
        private static bool _logAtLineStart = true;

        internal static void ConfigureTimestamps(string mode)
        {
            _timestamps = mode.ToLowerInvariant() switch
            {
                "abs" => TimestampMode.Absolute,
                "rel" => TimestampMode.Relative,
                _ => TimestampMode.Off,
            };

            _connectedAt = DateTime.UtcNow;
        }

        private static bool TimestampsEnabled => _timestamps != TimestampMode.Off;

        internal static string TimestampPrefix()
        {
            return _timestamps == TimestampMode.Absolute
                ? $"[{DateTime.Now:HH:mm:ss.fff}] "
                : $"[{(DateTime.UtcNow - _connectedAt).TotalSeconds,10:F3}] ";
        }

        // Walks the bytes a line at a time, handing each run to `write` and
        // asking for a prefix wherever a new line begins. A run that ends
        // without a newline leaves the caller mid line, so the next batch does
        // not get a second timestamp in the middle of it.
        internal static void WriteTimestamped(
            byte[] buffer, int count, ref bool atLineStart, Action<byte[], int, int> write, Action<string> writeText)
        {
            int index = 0;

            while (index < count)
            {
                if (atLineStart)
                {
                    writeText(TimestampPrefix());
                    atLineStart = false;
                }

                int newline = Array.IndexOf(buffer, (byte)'\n', index, count - index);

                if (newline < 0)
                {
                    write(buffer, index, count - index);
                    return;
                }

                write(buffer, index, newline - index + 1);
                atLineStart = true;
                index = newline + 1;
            }
        }

        internal static byte[] Ascii(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }
    }
}

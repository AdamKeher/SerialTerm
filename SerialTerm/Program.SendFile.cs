using System;
using System.IO;
using System.Text;
using System.Threading;

namespace TerminalConsole
{
    partial class Program
    {
        private static string _sendPath;
        private static int _sendDelay;
        internal static string _sendWait;
        private static int _sendTimeout;

        // The last bytes the device sent, so a send can wait for a prompt to
        // come back before pushing the next line. Only the tail matters, and a
        // prompt is short.
        private const int RecentSize = 256;

        private static readonly byte[] _recent = new byte[RecentSize];
        internal static int _recentCount;

        private static void ConfigureSend(CommandLineOptions options)
        {
            _sendPath = options.sendFile;
            _sendDelay = options.sendDelay;
            _sendWait = string.IsNullOrEmpty(options.sendWait) ? null : options.sendWait;
            _sendTimeout = options.sendTimeout;
        }

        internal static void NoteReceived(byte[] buffer, int count)
        {
            if (_sendWait == null)
                return;

            foreach (byte value in new ReadOnlySpan<byte>(buffer, 0, count))
            {
                if (_recentCount == RecentSize)
                {
                    Array.Copy(_recent, 1, _recent, 0, RecentSize - 1);
                    _recentCount--;
                }

                _recent[_recentCount++] = value;
            }
        }

        internal static bool RecentEndsWith(string text)
        {
            byte[] want = Encoding.ASCII.GetBytes(text);

            if (_recentCount < want.Length)
                return false;

            for (int index = 0; index < want.Length; index++)
                if (_recent[_recentCount - want.Length + index] != want[index])
                    return false;

            return true;
        }

        // Ctrl+A s. Prompts for a path, defaulting to --send-file.
        private static void SendFileCommand()
        {
            if (!_serialPort.IsOpen)
            {
                SayLine("\r\nNot connected.");
                return;
            }

            string prompt = _sendPath == null
                ? "\r\nSend file: "
                : $"\r\nSend file [{_sendPath}]: ";

            Say(prompt);

            string entry = Console.ReadLine()?.Trim();

            if (entry == null)
                return;

            if (entry.Length == 0)
                entry = _sendPath;

            if (string.IsNullOrEmpty(entry))
            {
                SayLine("Nothing sent.");
                return;
            }

            _sendPath = entry;
            SendFile(entry);
        }

        private static void SendFile(string path)
        {
            string[] lines;

            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                SayLine($"Cannot read {path}: {e.Message}");
                return;
            }

            SayLine($"Sending {lines.Length} lines from {Path.GetFullPath(path)}"
                + $"{(_sendDelay > 0 ? $", {_sendDelay}ms between lines" : string.Empty)}"
                + $"{(_sendWait != null ? $", waiting for '{_sendWait}'" : string.Empty)}"
                + $". Press {EscapeKeyName()} to stop.");

            int sent = 0;

            foreach (string line in lines)
            {
                if (CancelRequested())
                {
                    SayLine($"\r\nStopped after {sent} of {lines.Length} lines.");
                    return;
                }

                if (!_serialPort.IsOpen)
                {
                    SayLine($"\r\nDisconnected after {sent} of {lines.Length} lines.");
                    return;
                }

                lock (_consoleLock)
                    _recentCount = 0;

                SendToPort(Encoding.UTF8.GetBytes(line));
                SendToPort(_newlineBytes);
                sent++;

                if (_sendWait != null && !WaitForPrompt())
                {
                    SayLine($"\r\nTimed out after {sent} of {lines.Length} lines waiting for '{_sendWait}'.");
                    return;
                }

                if (_sendDelay > 0)
                    Thread.Sleep(_sendDelay);
            }

            SayLine($"\r\nSent {sent} lines.");
        }

        // A device with no flow control and a small receive buffer needs to be
        // let up for air. Waiting for its prompt is the reliable version of
        // that, a fixed delay the crude one.
        private static bool WaitForPrompt()
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(_sendTimeout);

            while (DateTime.UtcNow < deadline)
            {
                lock (_consoleLock)
                {
                    if (RecentEndsWith(_sendWait))
                        return true;
                }

                if (CancelRequested())
                    return false;

                Thread.Sleep(2);
            }

            return false;
        }

        // the escape key stops a send part way, without it also reaching the
        // device as a keystroke
        private static bool CancelRequested()
        {
            if (!KeyAvailable())
                return false;

            ConsoleKeyInfo key = Console.ReadKey(true);
            return key.KeyChar == _escapeKey || key.Key == ConsoleKey.Escape;
        }
    }
}

using System;
using System.IO;

namespace TerminalConsole
{
    partial class Program
    {
        private static FileStream _logStream;
        private static string _logPath;
        private static string _requestedLogPath;
        private static bool _logRaw;
        private static long _logBytes;

        private static void ConfigureLog(CommandLineOptions options)
        {
            _requestedLogPath = options.log;
            _logRaw = options.logRaw;

            if (_requestedLogPath != null)
                StartLog(_requestedLogPath);
        }

        // Ctrl+A l. With no --log given, name the file after the port and the
        // time, so capture can be started on the spur of the moment.
        private static void ToggleLog()
        {
            if (_logStream != null)
            {
                StopLog();
                return;
            }

            StartLog(_requestedLogPath ?? DefaultLogPath());
        }

        private static string DefaultLogPath()
        {
            return $"serialterm-{_serialPort.PortName}-{DateTime.Now:yyyyMMdd-HHmmss}.log";
        }

        private static void StartLog(string path)
        {
            try
            {
                // append, so toggling logging off and on again during a session
                // adds to the file rather than losing what came before
                _logStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                _logPath = path;
                _logBytes = 0;
                _ansiState = AnsiState.Text;

                SayLine($"\r\nLogging to {Path.GetFullPath(path)}{(_logRaw ? " (raw)" : string.Empty)}");
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                SayLine($"\r\nCannot log to {path}: {e.Message}");
                _logStream = null;
                _logPath = null;
            }
        }

        private static void StopLog()
        {
            if (_logStream == null)
                return;

            string path = _logPath;
            long written = _logBytes;

            CloseLog();

            SayLine($"\r\nStopped logging to {path}, {written} bytes written");
        }

        private static void CloseLog()
        {
            try
            {
                _logStream?.Dispose();
            }
            catch (IOException) { }

            _logStream = null;
            _logPath = null;
        }

        private static void LogBytes(byte[] buffer, int count)
        {
            if (_logStream == null)
                return;

            try
            {
                if (_logRaw)
                {
                    WriteToLog(buffer, 0, count);
                }
                else
                {
                    byte[] filtered = new byte[count];
                    int length = StripAnsi(buffer, count, filtered);

                    if (length > 0)
                        WriteToLog(filtered, 0, length);
                }

                _logStream.Flush();
            }
            catch (IOException e)
            {
                CloseLog();
                SayLine($"\r\nLogging stopped: {e.Message}");
            }
        }

        private static void WriteToLog(byte[] buffer, int offset, int count)
        {
            if (TimestampsEnabled)
                WriteTimestamped(buffer, count, ref _logAtLineStart,
                    (b, o, c) => { _logStream.Write(b, o, c); _logBytes += c; },
                    text => { byte[] stamp = Ascii(text); _logStream.Write(stamp, 0, stamp.Length); });
            else
            {
                _logStream.Write(buffer, offset, count);
                _logBytes += count;
            }
        }

        // Cursor movement and colour make a log unreadable and ungreppable, so
        // they come out by default. The filter is a state machine because a
        // sequence can be split across two reads from the port.
        private enum AnsiState { Text, Escape, Csi, Osc, OscEscape }

        private static AnsiState _ansiState = AnsiState.Text;

        private static int StripAnsi(byte[] input, int count, byte[] output)
        {
            int written = 0;

            for (int index = 0; index < count; index++)
            {
                byte value = input[index];

                switch (_ansiState)
                {
                    case AnsiState.Text:
                        if (value == Escape)
                            _ansiState = AnsiState.Escape;
                        else
                            output[written++] = value;
                        break;

                    case AnsiState.Escape:
                        _ansiState = value switch
                        {
                            (byte)'[' => AnsiState.Csi,
                            (byte)']' => AnsiState.Osc,
                            // any other two byte sequence, both bytes dropped
                            _ => AnsiState.Text,
                        };
                        break;

                    case AnsiState.Csi:
                        // parameters and intermediates run 0x20-0x3F, and the
                        // final byte that ends the sequence is 0x40-0x7E
                        if (value >= 0x40 && value <= 0x7E)
                            _ansiState = AnsiState.Text;
                        break;

                    case AnsiState.Osc:
                        // ends at BEL, or at ESC \ (the string terminator)
                        if (value == 0x07)
                            _ansiState = AnsiState.Text;
                        else if (value == Escape)
                            _ansiState = AnsiState.OscEscape;
                        break;

                    case AnsiState.OscEscape:
                        _ansiState = value == (byte)'\\' ? AnsiState.Text : AnsiState.Osc;
                        break;
                }
            }

            return written;
        }
    }
}

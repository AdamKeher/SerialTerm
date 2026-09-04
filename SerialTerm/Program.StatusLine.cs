using System;
using System.IO;
using System.IO.Ports;
using System.Text;

namespace TerminalConsole
{
    partial class Program
    {
        // Connection messages used to scroll away into the device's own output.
        // A reserved bottom row keeps the state visible instead.
        //
        // The row is reserved with DECSTBM, which confines scrolling to the rows
        // above it, so device output cannot land on it and the line never has to
        // be repainted over content. That also means the device sees one row
        // fewer than the window has, which a full screen program drawing to the
        // last row will get wrong - hence opt in.
        private static bool _statusEnabled;
        internal static int _statusRows;
        internal static int _statusColumns;
        private static DateTime _statusChecked = DateTime.MinValue;

        private const int StatusResizeInterval = 500;

        // Built from the byte rather than written as an escape. C#'s \x takes
        // one to four hex digits, so "\x1b7" is the single character U+01B7
        // instead of ESC followed by 7 - which silently turns DECSC and DECRC
        // into garbage, and leaves the cursor parked on the status row.
        private static readonly string Esc = ((char)Escape).ToString();

        private static void EnableStatusLine()
        {
            if (!_statusEnabled || !ConsoleGeometry(out int rows, out int columns))
                return;

            _statusRows = rows;
            _statusColumns = columns;

            lock (_consoleLock)
            {
                // DECSTBM homes the cursor, so put it back where it was, clamped
                // into the region that now exists
                int row = Math.Min(CursorRow(), rows - 1);
                int column = CursorColumn();

                WriteAnsi($"{Esc}[1;{rows - 1}r{Esc}[{row};{column}H");
            }

            PaintStatus();
        }

        private static void DisableStatusLine()
        {
            if (!_statusEnabled || _statusRows == 0)
                return;

            lock (_consoleLock)
            {
                // release the region and clear the row, so the shell prompt that
                // follows is not printed over a leftover status bar
                WriteAnsi($"{Esc}[r{Esc}[{_statusRows};1H{Esc}[K");
            }

            _statusRows = 0;
        }

        // The window can be resized at any point, which would leave the region
        // covering the wrong rows. Checked on a timer rather than every pass,
        // since reading the geometry is a syscall.
        private static void RefreshStatusLine()
        {
            if (!_statusEnabled)
                return;

            if ((DateTime.UtcNow - _statusChecked).TotalMilliseconds < StatusResizeInterval)
                return;

            _statusChecked = DateTime.UtcNow;

            if (!ConsoleGeometry(out int rows, out int columns))
                return;

            if (rows != _statusRows || columns != _statusColumns)
            {
                EnableStatusLine();
                return;
            }

            // repaint anyway, so a device that reset the region or drew over the
            // row does not leave it wrong forever
            if (!_hintVisible)
                PaintStatus();
        }

        private static void PaintStatus()
        {
            if (_statusEnabled && _statusRows > 0)
                PaintBottomLine(StatusText(_statusColumns));
        }

        internal static void PaintBottomLine(string text)
        {
            if (_statusRows <= 0)
                return;

            lock (_consoleLock)
            {
                if (text.Length > _statusColumns)
                    text = text.Substring(0, _statusColumns);

                // DECSC / DECRC rather than CSI s/u, which some terminals tie to
                // the scroll region
                WriteAnsi($"{Esc}7{Esc}[{_statusRows};1H{Esc}[7m{text.PadRight(_statusColumns)}{Esc}[0m{Esc}8");
            }
        }

        internal static string StatusText(int width)
        {
            var status = new StringBuilder(" ");

            status.Append(_serialPort.PortName);
            status.Append(' ').Append(_serialPort.BaudRate);
            status.Append(' ').Append(Framing());
            status.Append(_serialPort.IsOpen ? "  connected" : "  DISCONNECTED");

            status.Append("  DTR ").Append(_serialPort.DtrEnable ? "on" : "off");
            status.Append("  RTS ").Append(
                HandshakeOwnsRts() ? "hs" : _serialPort.RtsEnable ? "on" : "off");

            if (_logStream != null) status.Append("  LOG");
            if (_hexView) status.Append("  HEX");
            if (_frozen) status.Append("  FROZEN");
            if (_localEcho) status.Append("  ECHO");
            if (TimestampsEnabled) status.Append("  TIME");

            string text = status.ToString();
            string keys = $"{EscapeKeyName()} ? ";

            // right align the reminder if there is room for it
            if (text.Length + keys.Length + 2 <= width)
                text = text.PadRight(width - keys.Length) + keys;

            return text;
        }

        internal static string Framing()
        {
            char parity = _serialPort.Parity switch
            {
                Parity.None => 'N',
                Parity.Even => 'E',
                Parity.Odd => 'O',
                Parity.Mark => 'M',
                Parity.Space => 'S',
                _ => '?',
            };

            string stop = _serialPort.StopBits switch
            {
                StopBits.One => "1",
                StopBits.Two => "2",
                StopBits.OnePointFive => "1.5",
                _ => "?",
            };

            return $"{_serialPort.DataBits}{parity}{stop}";
        }

        private static void WriteAnsi(string text)
        {
            WriteRaw(Ascii(text), 0, text.Length);
        }

        private static bool ConsoleGeometry(out int rows, out int columns)
        {
            rows = 0;
            columns = 0;

            try
            {
                rows = Console.WindowHeight;
                columns = Console.WindowWidth;
            }
            catch (IOException)
            {
                return false;
            }

            return rows > 1 && columns > 0;
        }

        private static int CursorRow()
        {
            try
            {
                return Math.Max(1, Console.CursorTop - Console.WindowTop + 1);
            }
            catch (Exception)
            {
                return 1;
            }
        }

        private static int CursorColumn()
        {
            try
            {
                return Console.CursorLeft + 1;
            }
            catch (Exception)
            {
                return 1;
            }
        }
    }
}

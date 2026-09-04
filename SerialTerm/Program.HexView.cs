using System;
using System.Text;

namespace TerminalConsole
{
    partial class Program
    {
        private const int HexWidth = 16;

        private static bool _hexView;
        private static readonly byte[] _hexLine = new byte[HexWidth];
        private static int _hexCount;
        private static long _hexOffset;

        // Ctrl+A v. Not Ctrl+A x, which already quits.
        private static void ToggleHexView()
        {
            _hexView = !_hexView;

            if (!_hexView)
            {
                FlushHexLine();
                SayLine("\r\nHex view off");
                return;
            }

            _hexOffset = 0;
            _hexCount = 0;
            SayLine("\r\nHex view on, showing bytes as they arrive");
        }

        // Once a protocol is framed - Modbus, a sensor's register reads,
        // anything with a checksum - the text view shows mojibake and you are
        // down to guessing. Each read from the port ends its line, so the
        // boundaries between bursts stay visible, which is usually where the
        // frame boundaries are too.
        private static void RenderHex(byte[] buffer, int count)
        {
            for (int index = 0; index < count; index++)
            {
                _hexLine[_hexCount++] = buffer[index];

                if (_hexCount == HexWidth)
                    FlushHexLine();
            }

            FlushHexLine();
        }

        private static void FlushHexLine()
        {
            if (_hexCount == 0)
                return;

            var line = new StringBuilder(80);
            line.Append(_hexOffset.ToString("x8")).Append("  ");

            for (int column = 0; column < HexWidth; column++)
            {
                line.Append(column < _hexCount ? _hexLine[column].ToString("x2") : "  ");
                line.Append(column == HexWidth / 2 - 1 ? "  " : " ");
            }

            line.Append('|');

            for (int column = 0; column < _hexCount; column++)
            {
                byte value = _hexLine[column];
                line.Append(value >= 0x20 && value < 0x7F ? (char)value : '.');
            }

            line.Append("|\r\n");

            WriteRaw(Ascii(line.ToString()), 0, line.Length);

            _hexOffset += _hexCount;
            _hexCount = 0;
        }
    }
}

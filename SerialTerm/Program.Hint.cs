using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TerminalConsole
{
    partial class Program
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CHAR_INFO
        {
            public ushort UnicodeChar;
            public ushort Attributes;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadConsoleOutput(IntPtr hConsoleOutput, [Out] CHAR_INFO[] lpBuffer,
            COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpReadRegion);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO[] lpBuffer,
            COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpWriteRegion);

        // black on light grey, the usual look for a status line
        private const ushort HintAttributes = 0x70;

        private static CHAR_INFO[] _hintSaved;
        private static SMALL_RECT _hintRegion;
        private static bool _hintVisible;
        private static bool _hintEnabled = true;

        // The hint is painted straight into the console buffer rather than
        // written as text, so the cursor, the current colours and everything the
        // device has drawn are left exactly as they were. What was on the line is
        // kept and put back when the hint goes away.
        private static void ShowHint()
        {
            if (!_hintEnabled || !OperatingSystem.IsWindows() || _hintVisible)
                return;

            lock (_consoleLock)
            {
                try
                {
                    int width = Console.WindowWidth;
                    if (width <= 0)
                        return;

                    short row = (short)(Console.WindowTop + Console.WindowHeight - 1);
                    IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);

                    COORD size = new COORD { X = (short)width, Y = 1 };
                    COORD origin = new COORD { X = 0, Y = 0 };
                    _hintRegion = new SMALL_RECT { Left = 0, Top = row, Right = (short)(width - 1), Bottom = row };

                    _hintSaved = new CHAR_INFO[width];
                    SMALL_RECT readRegion = _hintRegion;
                    if (!ReadConsoleOutput(handle, _hintSaved, size, origin, ref readRegion))
                        return;

                    string text = HintText(width);
                    CHAR_INFO[] hint = new CHAR_INFO[width];
                    for (int column = 0; column < width; column++)
                    {
                        hint[column].UnicodeChar = column < text.Length ? text[column] : ' ';
                        hint[column].Attributes = HintAttributes;
                    }

                    SMALL_RECT writeRegion = _hintRegion;
                    _hintVisible = WriteConsoleOutput(handle, hint, size, origin, ref writeRegion);
                }
                catch (IOException) { }
                catch (ArgumentOutOfRangeException) { }
                catch (DllNotFoundException) { }
                catch (EntryPointNotFoundException) { }
            }
        }

        private static void HideHint()
        {
            if (!OperatingSystem.IsWindows() || !_hintVisible || _hintSaved == null)
                return;

            lock (_consoleLock)
            {
                try
                {
                    // The window can be resized, or the buffer scrolled by the
                    // device, between showing the hint and hiding it again.
                    // _hintRegion then names a different line, and writing the
                    // saved content back would paint a stale status line over
                    // live output. Leave it alone if the geometry moved - the
                    // next write from the device repaints that row anyway.
                    if (!HintRegionIsCurrent())
                        return;

                    COORD size = new COORD { X = (short)_hintSaved.Length, Y = 1 };
                    COORD origin = new COORD { X = 0, Y = 0 };
                    SMALL_RECT region = _hintRegion;

                    WriteConsoleOutput(GetStdHandle(STD_OUTPUT_HANDLE), _hintSaved, size, origin, ref region);
                }
                catch (DllNotFoundException) { }
                catch (EntryPointNotFoundException) { }
                finally
                {
                    _hintVisible = false;
                    _hintSaved = null;
                }
            }
        }

        private static bool HintRegionIsCurrent()
        {
            try
            {
                short row = (short)(Console.WindowTop + Console.WindowHeight - 1);
                return row == _hintRegion.Top && Console.WindowWidth == _hintSaved.Length;
            }
            catch (IOException)
            {
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private static string HintText(int width)
        {
            string prefix = EscapeKeyName();

            string full = $" {prefix}: ? help | d disc | i info | e esp32 | p pico | l log | v hex | f freeze | c clear | q quit ";
            if (full.Length <= width)
                return full;

            string brief = $" {prefix}: ? help | q quit ";
            if (brief.Length <= width)
                return brief;

            return full.Substring(0, width);
        }
    }
}

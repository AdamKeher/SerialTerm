using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TerminalConsole
{
    partial class Program
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_PROCESSED_OUTPUT = 0x0001;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        private static uint _originalConsoleMode;
        private static bool _consoleModeChanged;

        // Windows consoles do not interpret ANSI escape sequences unless the
        // program asks them to. Without this the cursor movement and colour a
        // device sends is printed literally, which is what leaves vi without its
        // status line and repaints in the wrong place.
        private static void EnableVirtualTerminal()
        {
            if (!OperatingSystem.IsWindows())
                return;

            try
            {
                IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);

                if (!GetConsoleMode(handle, out uint mode))
                    return;

                _originalConsoleMode = mode;
                _consoleModeChanged = SetConsoleMode(handle, mode | ENABLE_PROCESSED_OUTPUT | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        private static void RestoreConsoleMode()
        {
            if (!OperatingSystem.IsWindows() || !_consoleModeChanged)
                return;

            try
            {
                SetConsoleMode(GetStdHandle(STD_OUTPUT_HANDLE), _originalConsoleMode);
                _consoleModeChanged = false;
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        private static Encoding _originalOutputEncoding;
        private static bool _outputEncodingChanged;
        private static bool _utf8Output = true;

        // Device bytes are written to the console untouched, which is right -
        // decoding them first would corrupt anything outside plain ASCII. But
        // the console decides what those bytes mean by its output code page,
        // and on Windows that defaults to the OEM page: 437 or 850, not UTF-8.
        //
        // A device sending "44°C" puts C2 B0 on the wire, and CP437 renders
        // those two bytes as two glyphs, so the degree sign arrives as "44┬░C".
        // Telling the console the stream is UTF-8 is the whole fix; the bytes
        // were always right.
        private static void EnableUtf8Output()
        {
            if (!_utf8Output || Console.IsOutputRedirected)
                return;

            try
            {
                _originalOutputEncoding = Console.OutputEncoding;

                // no BOM, or every session would open by writing one
                Console.OutputEncoding = new UTF8Encoding(false);
                _outputEncodingChanged = true;
            }
            catch (Exception e) when (e is IOException || e is ArgumentException
                                   || e is PlatformNotSupportedException || e is NotSupportedException)
            {
                // a console that will not take UTF-8 still works, it just shows
                // the mojibake this was meant to fix
            }
        }

        private static void RestoreOutputEncoding()
        {
            if (!_outputEncodingChanged)
                return;

            try
            {
                Console.OutputEncoding = _originalOutputEncoding;
            }
            catch (Exception e) when (e is IOException || e is ArgumentException
                                   || e is PlatformNotSupportedException || e is NotSupportedException)
            {
                // leaving the console in UTF-8 is survivable, throwing on the
                // way out is not
            }
            finally
            {
                _outputEncodingChanged = false;
            }
        }
    }
}

using System;
using System.Runtime.InteropServices;

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
    }
}

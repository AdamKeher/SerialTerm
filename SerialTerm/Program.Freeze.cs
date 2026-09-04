using System;
using System.IO;

namespace TerminalConsole
{
    partial class Program
    {
        // A chatty device would grow this without limit, so the buffer is
        // capped. Past the cap the oldest bytes go, since the reason to freeze
        // is almost always to read something that just happened.
        internal const int FreezeBufferLimit = 1 << 20;

        internal static bool _frozen;
        internal static MemoryStream _freezeBuffer;
        internal static long _freezeDropped;

        // Ctrl+A f. Unlike Ctrl+A d, which closes the port, the device keeps
        // sending and nothing is lost - the screen simply stops moving, so a
        // stack trace can be read before it scrolls away.
        private static void ToggleFreeze()
        {
            if (_frozen)
            {
                Thaw();
                return;
            }

            _frozen = true;
            _freezeBuffer = new MemoryStream();
            _freezeDropped = 0;

            SayLine($"\r\nFrozen. {EscapeKeyName()} f to resume, output is still being captured.");
        }

        private static void Thaw()
        {
            _frozen = false;

            byte[] held = _freezeBuffer?.ToArray() ?? Array.Empty<byte>();
            long dropped = _freezeDropped;

            _freezeBuffer = null;
            _freezeDropped = 0;

            SayLine(dropped == 0
                ? $"\r\nResumed, {held.Length} bytes held while frozen."
                : $"\r\nResumed, {held.Length} bytes held while frozen, {dropped} dropped past the {FreezeBufferLimit} byte buffer.");

            if (held.Length > 0)
                RenderDeviceBytes(held, held.Length);
        }

        internal static void HoldWhileFrozen(byte[] buffer, int count)
        {
            _freezeBuffer.Write(buffer, 0, count);

            if (_freezeBuffer.Length <= FreezeBufferLimit)
                return;

            // keep the newest FreezeBufferLimit bytes
            byte[] all = _freezeBuffer.ToArray();
            int keep = FreezeBufferLimit;
            int drop = all.Length - keep;

            _freezeDropped += drop;
            _freezeBuffer = new MemoryStream();
            _freezeBuffer.Write(all, drop, keep);
        }
    }
}

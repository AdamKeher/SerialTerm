using System;
using System.Text;

namespace TerminalConsole
{
    partial class Program
    {
        private const byte Escape = 0x1B;
        private const byte Backspace = 0x08;
        private const byte Delete = 0x7F;
        private const byte CarriageReturn = 0x0D;
        private const byte LineFeed = 0x0A;

        // What Backspace and Enter put on the wire. Windows hands us 0x08 for
        // Backspace, but readline, nano, vi, BusyBox ash and the MicroPython
        // REPL all expect 0x7F and treat 0x08 as plain cursor-left - so the
        // key appears to move the cursor and delete nothing. DEL is the
        // default, matching PuTTY and every Unix terminal.
        private static byte[] _backspaceBytes = { Delete };
        private static byte[] _newlineBytes = { CarriageReturn };

        private static void SetLineDiscipline(string backspace, string newline)
        {
            _backspaceBytes = backspace.Equals("bs", StringComparison.OrdinalIgnoreCase)
                ? new[] { Backspace }
                : new[] { Delete };

            _newlineBytes = newline.ToLowerInvariant() switch
            {
                "lf" => new[] { LineFeed },
                "crlf" => new[] { CarriageReturn, LineFeed },
                _ => new[] { CarriageReturn },
            };
        }

        // Keys that carry no character of their own (arrows, navigation and
        // function keys) are translated into the sequence a real terminal would
        // send, so full screen applications on the device - vi, nano, htop -
        // receive the key that was actually pressed instead of a null byte.
        private static byte[] EncodeKey(ConsoleKeyInfo key)
        {
            int modifier = 1
                + ((key.Modifiers & ConsoleModifiers.Shift) != 0 ? 1 : 0)
                + ((key.Modifiers & ConsoleModifiers.Alt) != 0 ? 2 : 0)
                + ((key.Modifiers & ConsoleModifiers.Control) != 0 ? 4 : 0);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow: return CursorKey('A', modifier);
                case ConsoleKey.DownArrow: return CursorKey('B', modifier);
                case ConsoleKey.RightArrow: return CursorKey('C', modifier);
                case ConsoleKey.LeftArrow: return CursorKey('D', modifier);
                case ConsoleKey.Home: return CursorKey('H', modifier);
                case ConsoleKey.End: return CursorKey('F', modifier);

                case ConsoleKey.Insert: return EditingKey(2, modifier);
                case ConsoleKey.Delete: return EditingKey(3, modifier);
                case ConsoleKey.PageUp: return EditingKey(5, modifier);
                case ConsoleKey.PageDown: return EditingKey(6, modifier);

                case ConsoleKey.F1: return FunctionKey('P', modifier);
                case ConsoleKey.F2: return FunctionKey('Q', modifier);
                case ConsoleKey.F3: return FunctionKey('R', modifier);
                case ConsoleKey.F4: return FunctionKey('S', modifier);
                case ConsoleKey.F5: return EditingKey(15, modifier);
                case ConsoleKey.F6: return EditingKey(17, modifier);
                case ConsoleKey.F7: return EditingKey(18, modifier);
                case ConsoleKey.F8: return EditingKey(19, modifier);
                case ConsoleKey.F9: return EditingKey(20, modifier);
                case ConsoleKey.F10: return EditingKey(21, modifier);
                case ConsoleKey.F11: return EditingKey(23, modifier);
                case ConsoleKey.F12: return EditingKey(24, modifier);

                case ConsoleKey.Tab when (key.Modifiers & ConsoleModifiers.Shift) != 0:
                    return Sequence("[Z");

                // both carry a KeyChar, but which byte it should be is the
                // user's to choose rather than whatever Windows hands us
                case ConsoleKey.Backspace: return WithMeta(_backspaceBytes, key);
                case ConsoleKey.Enter: return WithMeta(_newlineBytes, key);

                default:
                    break;
            }

            // keys with no character and no sequence of their own send nothing,
            // rather than the null byte earlier versions wrote to the port
            if (key.KeyChar == '\0')
                return Array.Empty<byte>();

            return WithMeta(Encoding.UTF8.GetBytes(key.KeyChar.ToString()), key);
        }

        // Alt is sent as a leading escape, the usual meta convention
        private static byte[] WithMeta(byte[] character, ConsoleKeyInfo key)
        {
            if ((key.Modifiers & ConsoleModifiers.Alt) == 0)
                return character;

            byte[] meta = new byte[character.Length + 1];
            meta[0] = Escape;
            Array.Copy(character, 0, meta, 1, character.Length);
            return meta;
        }

        // ESC [ A .. ESC [ D, ESC [ H, ESC [ F -- with ESC [ 1 ; modifier X when held
        private static byte[] CursorKey(char final, int modifier)
        {
            return modifier == 1
                ? Sequence($"[{final}")
                : Sequence($"[1;{modifier}{final}");
        }

        // ESC O P .. ESC O S for F1 - F4, the vt100 style application keys
        private static byte[] FunctionKey(char final, int modifier)
        {
            return modifier == 1
                ? Sequence($"O{final}")
                : Sequence($"[1;{modifier}{final}");
        }

        // ESC [ n ~ for the editing and higher function keys
        private static byte[] EditingKey(int number, int modifier)
        {
            return modifier == 1
                ? Sequence($"[{number}~")
                : Sequence($"[{number};{modifier}~");
        }

        private static byte[] Sequence(string body)
        {
            byte[] bytes = new byte[body.Length + 1];
            bytes[0] = Escape;

            for (int index = 0; index < body.Length; index++)
                bytes[index + 1] = (byte)body[index];

            return bytes;
        }
    }
}

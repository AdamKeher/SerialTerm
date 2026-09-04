using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TerminalConsole
{
    partial class Program
    {
        // Slot '1'..'9' to the bytes it sends. With full key passthrough the
        // function keys belong to the device, so macros live behind the escape
        // key - Ctrl+A 1 .. Ctrl+A 9, the way screen does it.
        private static readonly Dictionary<char, byte[]> _macros = new();
        private static readonly Dictionary<char, string> _macroText = new();

        private static void ConfigureMacros(string[] definitions)
        {
            if (definitions == null)
                return;

            foreach (string definition in definitions)
            {
                int equals = definition.IndexOf('=');

                if (equals < 1)
                {
                    SayLine($"Ignoring macro '{definition}', expected a form like 1=reboot\\r");
                    continue;
                }

                string slot = definition.Substring(0, equals);
                string text = definition.Substring(equals + 1);

                if (slot.Length != 1 || slot[0] < '1' || slot[0] > '9')
                {
                    SayLine($"Ignoring macro '{definition}', the slot must be a digit 1-9");
                    continue;
                }

                _macros[slot[0]] = Unescape(text);
                _macroText[slot[0]] = text;
            }
        }

        private static bool RunMacro(char slot)
        {
            if (!_macros.TryGetValue(slot, out byte[] data))
                return false;

            SendToPort(data);
            return true;
        }

        private static bool HasMacros => _macros.Count > 0;

        private static IEnumerable<KeyValuePair<char, string>> MacroList()
        {
            var slots = new List<char>(_macroText.Keys);
            slots.Sort();

            foreach (char slot in slots)
                yield return new KeyValuePair<char, string>(slot, _macroText[slot]);
        }

        // A macro is nearly always a command plus a carriage return, so the
        // usual escapes are understood rather than requiring the shell to
        // produce control characters.
        internal static byte[] Unescape(string text)
        {
            var bytes = new List<byte>(text.Length);

            for (int index = 0; index < text.Length; index++)
            {
                if (text[index] != '\\' || index + 1 >= text.Length)
                {
                    bytes.AddRange(Encoding.UTF8.GetBytes(text[index].ToString()));
                    continue;
                }

                char next = text[++index];

                switch (next)
                {
                    case 'r': bytes.Add(0x0D); break;
                    case 'n': bytes.Add(0x0A); break;
                    case 't': bytes.Add(0x09); break;
                    case '0': bytes.Add(0x00); break;
                    case 'e': bytes.Add(Escape); break;
                    case '\\': bytes.Add((byte)'\\'); break;

                    // \xNN, a literal byte
                    case 'x':
                    case 'X':
                        if (index + 2 < text.Length
                            && byte.TryParse(text.Substring(index + 1, 2), NumberStyles.HexNumber,
                                CultureInfo.InvariantCulture, out byte value))
                        {
                            bytes.Add(value);
                            index += 2;
                        }
                        else
                            bytes.Add((byte)next);
                        break;

                    default:
                        // unknown escape, keep both characters as typed
                        bytes.Add((byte)'\\');
                        bytes.AddRange(Encoding.UTF8.GetBytes(next.ToString()));
                        break;
                }
            }

            return bytes.ToArray();
        }
    }
}

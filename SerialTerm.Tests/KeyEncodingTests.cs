using System;
using System.Text;
using TerminalConsole;
using Xunit;

namespace SerialTerm.Tests
{
    // The key encoder is what makes vi, nano and htop usable over the link, so
    // the exact bytes matter. ESC is written as (char)0x1B rather than an
    // escape, for the same reason the status line does.
    public class KeyEncodingTests
    {
        private const byte Esc = 0x1B;

        private static byte[] Encode(ConsoleKey key, char character = '\0', ConsoleModifiers modifiers = 0)
        {
            return Program.EncodeKey(new ConsoleKeyInfo(
                character, key,
                (modifiers & ConsoleModifiers.Shift) != 0,
                (modifiers & ConsoleModifiers.Alt) != 0,
                (modifiers & ConsoleModifiers.Control) != 0));
        }

        private static byte[] Bytes(params object[] parts)
        {
            var bytes = new System.Collections.Generic.List<byte>();

            foreach (object part in parts)
            {
                if (part is string text) bytes.AddRange(Encoding.ASCII.GetBytes(text));
                else if (part is char c) bytes.Add((byte)c);
                else bytes.Add(Convert.ToByte(part));
            }

            return bytes.ToArray();
        }

        [Fact]
        public void ArrowKeysSendCursorSequences()
        {
            Assert.Equal(Bytes(Esc, "[A"), Encode(ConsoleKey.UpArrow));
            Assert.Equal(Bytes(Esc, "[B"), Encode(ConsoleKey.DownArrow));
            Assert.Equal(Bytes(Esc, "[C"), Encode(ConsoleKey.RightArrow));
            Assert.Equal(Bytes(Esc, "[D"), Encode(ConsoleKey.LeftArrow));
        }

        [Fact]
        public void ModifiedArrowsCarryTheModifier()
        {
            // Ctrl is 4, plus the base of 1
            Assert.Equal(Bytes(Esc, "[1;5A"), Encode(ConsoleKey.UpArrow, modifiers: ConsoleModifiers.Control));
            Assert.Equal(Bytes(Esc, "[1;2A"), Encode(ConsoleKey.UpArrow, modifiers: ConsoleModifiers.Shift));
            Assert.Equal(Bytes(Esc, "[1;3A"), Encode(ConsoleKey.UpArrow, modifiers: ConsoleModifiers.Alt));
        }

        [Fact]
        public void FunctionKeysUseTheirVt100Forms()
        {
            Assert.Equal(Bytes(Esc, "OP"), Encode(ConsoleKey.F1));
            Assert.Equal(Bytes(Esc, "OS"), Encode(ConsoleKey.F4));
            Assert.Equal(Bytes(Esc, "[15~"), Encode(ConsoleKey.F5));
            Assert.Equal(Bytes(Esc, "[24~"), Encode(ConsoleKey.F12));
        }

        [Fact]
        public void EditingKeysUseTheirNumberedForms()
        {
            Assert.Equal(Bytes(Esc, "[2~"), Encode(ConsoleKey.Insert));
            Assert.Equal(Bytes(Esc, "[3~"), Encode(ConsoleKey.Delete));
            Assert.Equal(Bytes(Esc, "[5~"), Encode(ConsoleKey.PageUp));
            Assert.Equal(Bytes(Esc, "[6~"), Encode(ConsoleKey.PageDown));
        }

        [Fact]
        public void ShiftTabSendsBackTab()
        {
            Assert.Equal(Bytes(Esc, "[Z"), Encode(ConsoleKey.Tab, '\t', ConsoleModifiers.Shift));
        }

        [Fact]
        public void PlainCharactersPassThrough()
        {
            Assert.Equal(new byte[] { (byte)'a' }, Encode(ConsoleKey.A, 'a'));
        }

        [Fact]
        public void AltPrefixesEscape()
        {
            Assert.Equal(Bytes(Esc, "a"), Encode(ConsoleKey.A, 'a', ConsoleModifiers.Alt));
        }

        [Fact]
        public void KeysWithNoCharacterSendNothing()
        {
            // earlier versions wrote a null byte to the port for these
            Assert.Empty(Encode(ConsoleKey.LeftWindows));
        }

        [Fact]
        public void UnicodeIsSentAsUtf8()
        {
            Assert.Equal(new byte[] { 0xC3, 0xA9 }, Encode(ConsoleKey.NoName, 'é'));
        }

        [Theory]
        [InlineData("del", 0x7F)]
        [InlineData("bs", 0x08)]
        public void BackspaceHonoursTheLineDiscipline(string setting, byte expected)
        {
            Program.SetLineDiscipline(setting, "cr");
            Assert.Equal(new[] { expected }, Encode(ConsoleKey.Backspace, '\b'));
        }

        [Theory]
        [InlineData("cr", new byte[] { 0x0D })]
        [InlineData("lf", new byte[] { 0x0A })]
        [InlineData("crlf", new byte[] { 0x0D, 0x0A })]
        public void EnterHonoursTheLineDiscipline(string setting, byte[] expected)
        {
            Program.SetLineDiscipline("del", setting);
            Assert.Equal(expected, Encode(ConsoleKey.Enter, '\r'));
        }

        [Fact]
        public void AltBackspaceStillGetsItsMetaPrefix()
        {
            Program.SetLineDiscipline("del", "cr");
            Assert.Equal(new byte[] { Esc, 0x7F }, Encode(ConsoleKey.Backspace, '\b', ConsoleModifiers.Alt));
        }
    }
}

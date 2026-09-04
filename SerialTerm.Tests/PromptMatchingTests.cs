using System.Text;
using TerminalConsole;
using Xunit;

namespace SerialTerm.Tests
{
    // Waiting for the device's prompt is the reliable way to pace an upload, so
    // the matcher has to cope with a prompt arriving split across reads and with
    // the tail buffer wrapping.
    public class PromptMatchingTests
    {
        private static void Reset()
        {
            Program._sendWait = ">>> ";
            Program._recentCount = 0;
        }

        private static void Receive(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            Program.NoteReceived(bytes, bytes.Length);
        }

        [Fact]
        public void MatchesAnExactPrompt()
        {
            Reset();
            Receive(">>> ");
            Assert.True(Program.RecentEndsWith(">>> "));
        }

        [Fact]
        public void MatchesAPromptAfterOutput()
        {
            Reset();
            Receive("print(1)\r\n1\r\n>>> ");
            Assert.True(Program.RecentEndsWith(">>> "));
        }

        [Fact]
        public void DoesNotMatchBeforeThePromptArrives()
        {
            Reset();
            Receive("some output\r\n");
            Assert.False(Program.RecentEndsWith(">>> "));
        }

        [Fact]
        public void DoesNotMatchWhenThePromptIsNotAtTheEnd()
        {
            Reset();
            Receive(">>> extra");
            Assert.False(Program.RecentEndsWith(">>> "));
        }

        [Fact]
        public void MatchesAPromptSplitAcrossReads()
        {
            Reset();
            Receive("done\r\n>>");
            Assert.False(Program.RecentEndsWith(">>> "));

            Receive("> ");
            Assert.True(Program.RecentEndsWith(">>> "));
        }

        [Fact]
        public void MatchesAPromptArrivingOneByteAtATime()
        {
            Reset();

            foreach (char character in "ok\r\n>>> ")
                Receive(character.ToString());

            Assert.True(Program.RecentEndsWith(">>> "));
        }

        [Fact]
        public void KeepsMatchingAfterTheBufferWraps()
        {
            Reset();
            Receive(new string('x', 600));
            Receive(">>> ");
            Assert.True(Program.RecentEndsWith(">>> "));
        }

        [Fact]
        public void StopsMatchingOnceThePromptScrollsPast()
        {
            Reset();
            Receive(">>> ");
            Receive("more output");
            Assert.False(Program.RecentEndsWith(">>> "));
        }

        [Theory]
        [InlineData("uboot=> ", "=> ")]
        [InlineData("$ ", "$ ")]
        [InlineData("login: ", "login: ")]
        [InlineData("OK\r\n", "OK\r\n")]
        public void MatchesOtherPromptShapes(string received, string prompt)
        {
            Reset();
            Receive(received);
            Assert.True(Program.RecentEndsWith(prompt));
        }
    }
}

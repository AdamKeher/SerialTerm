namespace TerminalConsole
{
    partial class Program
    {
        private static bool _localEcho;

        // Half duplex devices and raw AT command modems echo nothing back, so
        // without this you type blind. Ctrl+A o.
        private static void ToggleLocalEcho()
        {
            _localEcho = !_localEcho;
            SayLine($"\r\nLocal echo {(_localEcho ? "on" : "off")}");
        }

        // Goes through the same renderer as device output, so echoed bytes obey
        // whichever view is current - in hex view you see what you sent in hex
        // too, interleaved with what came back.
        private static void EchoLocally(byte[] data)
        {
            if (_localEcho)
                RenderDeviceBytes(data, data.Length);
        }
    }
}

using System;
using System.IO;
using System.Threading;

namespace TerminalConsole
{
    partial class Program
    {
        // Ctrl+A t. Shows both lines and lets either be flipped, for bringing
        // up a board whose reset circuit is not the usual one.
        private static void LineControlCommand()
        {
            if (!_serialPort.IsOpen)
            {
                SayLine("\r\nNot connected.");
                return;
            }

            SayBlock(() =>
            {
                SayLine($"\r\nDTR {(_serialPort.DtrEnable ? "on" : "off")}"
                    + $"   RTS {(HandshakeOwnsRts() ? "driven by handshake" : _serialPort.RtsEnable ? "on" : "off")}");
                Say("Toggle (d)tr, (r)ts, or blank to leave alone: ");
            });

            string entry = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(entry))
                return;

            try
            {
                switch (entry)
                {
                    case "d":
                    case "dtr":
                        _serialPort.DtrEnable = !_serialPort.DtrEnable;
                        SayLine($"DTR now {(_serialPort.DtrEnable ? "on" : "off")}.");
                        break;

                    case "r":
                    case "rts":
                        if (HandshakeOwnsRts())
                        {
                            SayLine($"RTS is driven by --handshake {_serialPort.Handshake}, leaving it alone.");
                            break;
                        }

                        _serialPort.RtsEnable = !_serialPort.RtsEnable;
                        SayLine($"RTS now {(_serialPort.RtsEnable ? "on" : "off")}.");
                        break;

                    default:
                        SayLine($"'{entry}' is not d or r.");
                        break;
                }
            }
            catch (Exception e) when (e is InvalidOperationException || e is IOException)
            {
                SayLine($"Could not change the line: {e.Message}");
            }
        }

        // Ctrl+A B, distinct from Ctrl+A b which sends a break.
        //
        // The usual dev board circuit puts DTR on IO0 and RTS on EN through a
        // transistor pair. Ctrl+A e toggles RTS alone, which resets the chip
        // into its normal firmware. Entering the ROM downloader means holding
        // IO0 low across the reset, so both lines have to move together - a
        // different operation, and the one esptool performs before flashing.
        private static void Esp32BootloaderCommand()
        {
            if (!_serialPort.IsOpen)
            {
                SayLine("\r\nNot connected.");
                return;
            }

            if (HandshakeOwnsRts())
            {
                SayLine($"\r\nCannot drive RTS while --handshake is set to {_serialPort.Handshake}.");
                return;
            }

            Say("\r\nESP32 download mode. Holding IO0 low across reset ... ");

            try
            {
                // IO0 low, EN low - chip held in reset with boot pin asserted
                _serialPort.DtrEnable = false;
                _serialPort.RtsEnable = true;
                Thread.Sleep(100);

                // release EN, chip boots and samples IO0, which is still low
                _serialPort.DtrEnable = true;
                _serialPort.RtsEnable = false;
                Thread.Sleep(50);

                // release IO0
                _serialPort.DtrEnable = false;

                SayLine("done. The chip should be waiting for a download.");
            }
            catch (Exception e) when (e is InvalidOperationException || e is IOException)
            {
                SayLine($"failed: {e.Message}");
            }
        }
    }
}

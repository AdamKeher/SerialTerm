# SerialTerm

A simple serial terminal for the command line, built for working on electronics.

Download a self contained single file build @ https://github.com/AdamKeher/SerialTerm/releases —
there is no runtime to install. Builds are published for Windows, Linux and
macOS, on both x64 and arm64.

On Windows, [Windows Terminal](https://github.com/microsoft/terminal) is worth
having for its VT100 support.

## About

SerialTerm was created to provide a simple command line driven serial port
listener with VT100 / ANSI support for use with my electronics projects. It has
since grown the things a bench actually needs: capturing a session to a file,
timestamping a boot log, reading a framed protocol in hex, pushing a script onto
a board, and finding the right port when the COM number keeps moving.

### Features

**Terminal**

* Full key pass through, so `vi`, `nano` and `htop` work over the connection
* ESC, the function keys, the arrows and Ctrl+C all reach the device; SerialTerm's
  own commands sit behind an escape key
* Uses the terminal's native VT100 / ANSI support
* Optional status line showing the connection at a glance
* Light weight implementation, simple single file deployment

**Capturing what the device sends**

* Log a session to a file, started from the command line or mid session
* ANSI escape sequences stripped from the log by default, so it stays greppable
* Line timestamps, absolute or relative to connecting
* Hex view for binary and framed protocols
* Freeze the screen to read a stack trace, without losing what arrives meanwhile

**Sending to the device**

* Send a text file line by line, paced by a delay or by waiting for the prompt
* Macros bound to `Ctrl+A 1` – `Ctrl+A 9`
* Send a break, for U-Boot, SysRq and bootloaders
* Local echo, for devices that do not echo back
* Configurable Backspace and Enter bytes

**Connection**

* Pick the port by device description or VID:PID rather than COM number
* Named argument profiles, so a board is one flag away
* Change baud rate without restarting
* Survives physical disconnection and reconnection without exiting
* Quickly close and reopen the port, to hand it to a third party flash tool
* Soft reset an ESP32, or enter its ROM download mode
* Reset a Raspberry Pi Pico to programming mode
* Manual DTR and RTS control

## Quick start

```
SerialTerm                                  pick from the ports that are present
SerialTerm -P COM3 -b 9600                  a specific port and rate
SerialTerm --match CP210x                   whichever port that board is on today
SerialTerm @esp32                           a saved profile
SerialTerm list                             what is plugged in
```

Something closer to a working session:

```
SerialTerm --match CP210x --reset-esp32 --timestamp rel --log boot.log --status-line
```

## Terminal keys

SerialTerm sends every key to the connected device, including ESC, the function
keys, the arrows and Ctrl+C. Its own commands sit behind an escape key, `Ctrl+A`
by default: press the escape key, then a command key.

```
Ctrl+A ?          Display SerialTerm key help
Ctrl+A d          Disconnect / Reconnect serial connection
Ctrl+A i          Display serial port settings
Ctrl+A #          Change the baud rate without restarting
Ctrl+A e          Soft reset ESP32 by toggling RTS enabled
Ctrl+A B          Put an ESP32 into download mode, IO0 held low across reset
Ctrl+A t          Show and toggle the DTR and RTS lines
Ctrl+A p          Reset PICO to programming mode by toggling 1200 baud connection
Ctrl+A l          Start / stop logging the session to a file
Ctrl+A v          Toggle hex view of incoming bytes
Ctrl+A f          Freeze / resume the screen, output keeps being captured
Ctrl+A b          Send a break to the device
Ctrl+A o          Toggle local echo of what you type
Ctrl+A s          Send a text file to the device line by line
Ctrl+A 1-9        Send a macro defined with --macro
Ctrl+A c          Clear terminal screen
Ctrl+A q          Exit terminal program
Ctrl+A Ctrl+A     Send a literal Ctrl+A to the connected device
```

Note the case: `Ctrl+A b` sends a break, `Ctrl+A B` enters ESP32 download mode.
That is the only binding where case matters.

Pressing the escape key paints a hint line along the bottom of the window
listing the commands, and puts back whatever was underneath it as soon as a
command key is pressed. It is drawn straight into the console buffer, so it
does not disturb the cursor, the colours, or anything a full screen program on
the device has drawn. Pass `--no-hint` to turn it off.

That floating hint is Windows only, because reading back what is on the screen
in order to restore it afterwards has no ANSI equivalent. `--status-line` gives
the same reminder on every platform, on a row reserved for it.

Use `--escape-key` to move it somewhere else, for example `--escape-key ^]` for
the telnet escape character. `--legacy-keys` additionally restores the original
F1 - F5, Home and ESC shortcuts, but with it ESC no longer reaches the device.

# Connecting

## Picking a port

COM numbers shuffle between reboots and hubs; what is plugged in does not.
`--match` selects the port whose name or device description contains the text:

```
SerialTerm --match CP210x
SerialTerm --match 10C4:EA60
SerialTerm --match COM7
```

If nothing matches yet it waits for the device to appear, so the command can be
run before the board is plugged in. If several match it lists them and asks.

`-P auto` asks for the same automatic selection as leaving `--port` off, which
is worth being able to say explicitly in a script.

## list

`list` names the device behind each port, read from the Windows device tree.
No port is opened, so nothing attached is disturbed.

```
Serial Ports
------------
#  Port  Device
1  COM3  Silicon Labs CP210x USB to UART Bridge  10C4:EA60
2  COM4  USB Serial Device  16C0:0483
```

Pass `--probe` to also report whether each port is free. That works by opening
each port in turn, which asserts DTR and RTS and so **resets any board wired
for auto reset** - every ESP32 dev board, and Arduinos with the reset
capacitor. It is off by default for that reason.

## Profiles

A bench has the same few boards on it every day, so arguments can be saved and
recalled by name. Put one argument per line in
`%APPDATA%\SerialTerm\profiles\esp32.rsp` (`~/.config/SerialTerm/profiles`
elsewhere):

```
--match
CP210x
--baud
115200
--reset-esp32
--status-line
```

then run `SerialTerm @esp32`. `SerialTerm profiles` lists what is saved.

`@` also takes a path, so a project can keep its settings in its own repository
as `@./serial.rsp`. A local file of that name wins over a profile, so a
project's own settings are never shadowed.

## Changing baud rate

`Ctrl+A #` lists the common rates and takes either a number from the list or an
arbitrary rate. Chasing an unknown rate no longer means quitting and relaunching
for each guess.

The rate can only be changed on a closed port, so the connection is dropped and
remade around it. If the driver refuses the rate the previous one is restored
and the connection comes back.

# Capturing the stream

## Logging

`--log session.log` appends everything the device sends to a file. `Ctrl+A l`
starts and stops it mid session - with no `--log` given it picks a name from the
port and the time, so capture can be started the moment something interesting
happens.

ANSI escape sequences are stripped so the log stays readable and greppable;
`--log-raw` keeps them. The file is opened in append mode, so stopping and
restarting during a session adds to it rather than truncating.

## Timestamps

`--timestamp rel` prefixes each line from the device with seconds since
connecting, `--timestamp abs` with the wall clock:

```
[     0.000] boot: ESP-IDF v5.1
[     2.310] wifi: connected
[    14.882] E (14882) watchdog: task not resetting
```

This is how you find out the watchdog fires 14.9 s after boot. Timestamps go to
the log as well as the screen.

They assume line oriented output. A full screen program on the device draws with
cursor positioning rather than lines, so leave timestamps off while running one.

## Hex view

`Ctrl+A v` switches the incoming stream to an offset / hex / ASCII dump:

```
00000000  01 03 00 00 00 02 c4 0b                          |........|
00000008  01 03 04 00 0a 00 14 da  31                      |........1|
00000011  64 6f 6e 65 0d 0a                                |done..|
```

Each read from the port ends its line, so the boundaries between bursts stay
visible - which for a framed protocol is usually where the frame boundaries are.
Offsets run from when the view was switched on.

It is `Ctrl+A v` rather than the more obvious `Ctrl+A x` because `x` already
quits.

## Freeze

`Ctrl+A f` stops the screen moving so a stack trace can be read before the
device scrolls it away. The port stays open and the bytes keep being collected -
this is not `Ctrl+A d`, which closes the connection and loses whatever arrives.
Press it again to resume, and everything held is painted in order.

The buffer is capped at 1 MB. Past that the oldest bytes go and the resume
message says how many, since the reason to freeze is almost always to read
something that just happened.

# Sending to the device

## Sending a file

`Ctrl+A s` prompts for a path and sends it line by line; `--send-file` sends one
straight after connecting. This is how a MicroPython or CircuitPython script
gets onto a board over the REPL, and how a config script gets replayed.

Lines go out with whatever `--newline` is set to. Press the escape key during a
send to stop part way.

A device with no flow control and a small receive buffer will drop lines if you
push them as fast as the port allows. Two ways to pace it:

```
SerialTerm -P COM3 --send-file setup.py --send-delay 20
SerialTerm -P COM3 --send-file setup.py --send-wait ">>> "
```

`--send-delay` waits a fixed number of milliseconds between lines, which is
crude but needs to know nothing about the device. `--send-wait` waits for the
device's prompt to come back before sending the next line, which is the reliable
version - it gives up after `--send-timeout` milliseconds, 2000 by default, and
says which line it stopped on.

## Macros

`--macro` binds text to `Ctrl+A 1` through `Ctrl+A 9`, and is repeatable:

```
SerialTerm -P COM3 --macro "1=reboot\r" --macro "2=\x03" --macro "3=\e[2J"
```

With full key passthrough the function keys belong to the device, so macros live
behind the escape key, the way `screen` does it. The defined ones are listed by
`Ctrl+A ?` alongside the built in keys.

The text understands `\r`, `\n`, `\t`, `\0`, `\e`, `\\` and `\xNN` for an
arbitrary byte, since a macro is nearly always a command plus a carriage return.

## Break

`Ctrl+A b` holds the line in a break condition for 250 ms. A break is a run of
zero bits longer than a character frame, so no sequence of bytes can produce one -
it needs the driver. It interrupts U-Boot, reaches Linux SysRq, and drops some
bootloaders into command mode.

## Local echo

Half duplex devices and raw AT command modems echo nothing back, so you type
blind. `--local-echo`, or `Ctrl+A o` during a session, shows what you send.

Echoed bytes go through the same renderer as device output, so they obey
whichever view is current - in hex view you see what you sent in hex as well,
interleaved with what came back.

## Character encoding

Device output is written to the console as raw bytes, and the console decides
what they mean by its output code page. On Windows that defaults to the OEM page
- 437 or 850 - not UTF-8, so a device reporting `44°C` sends `C2 B0` and the
console draws those two bytes as two glyphs: `44┬░C`.

SerialTerm switches the console to UTF-8 for the session and puts the previous
code page back on exit. Nothing about the bytes changes; the console is simply
told what they are.

For a device that sends its own 8 bit encoding rather than UTF-8 - Latin-1 text,
or CP437 box drawing - `--no-utf8` leaves the code page alone.

## Backspace and Enter

Backspace sends DEL (`0x7F`), which is what readline, `nano`, `vi`, BusyBox ash
and the MicroPython REPL expect. Devices that want the older BS (`0x08`) instead
take `--backspace bs`.

Enter sends CR (`0x0D`). Use `--newline lf` or `--newline crlf` for devices that
want a line feed.

# Board control

## DTR, RTS and ESP32 download mode

`Ctrl+A t` shows both control lines and lets either be flipped by hand, for
bringing up a board whose reset circuit is not the usual one.

`Ctrl+A B` puts an ESP32 into its ROM downloader. Note the capital: `Ctrl+A b`
sends a break, `Ctrl+A B` enters download mode.

`Ctrl+A e` and `Ctrl+A B` are also different from each other. The usual dev
board circuit puts DTR on IO0 and RTS on EN through a transistor pair. `Ctrl+A e`
toggles RTS alone, which resets the chip into its normal firmware. Entering the
downloader means holding IO0 low across the reset, so both lines have to move
together - which is what esptool does before flashing.

## Status line

`--status-line` reserves the bottom row for the connection state, so
"Disconnected." and "Reconnected." stop scrolling away into the device's own
output:

```
 COM3 115200 8N1  connected  DTR on  RTS off  LOG  HEX              Ctrl+A ?
```

It shows the port, baud, framing, connection state, both control lines, and
whichever of logging, hex view, freeze, local echo and timestamps are on.

The row is reserved with a DECSTBM scroll region, so device output scrolls above
it and can never overwrite it. That is also why it is off by default: the device
gets one row fewer than the window has, and a full screen program on the device
that draws to the last row will get it wrong.

While the status line is on, the escape key hint shares the same row instead of
saving and restoring what was underneath.

# Syntax

```
Description:
  SerialTerm - Simple serial port terminal program. (c)2021 AKsevenFour - https://github.com/AdamKeher/SerialTerm

Usage:
  SerialTerm [command] [options]

Options:
  -P, --port <port>                               Set the serial port to listen on
  -M, --match <match>                             Pick the port whose name or device description contains this text, eg. --match CP210x
  -b, --baud <baud>                               Set serial port baud rate [default: 115200]
  -de, --disconnect-exit                          Exit terminal on disconnection [default: False]
  -r, --reset-esp32                               Reset ESP32 on connection [default: False]
  -dtr, --disable-dtr                             Disable DTR for serial connection [default: False]
  -rts, --disable-rts                             Disable RTS for serial connection [default: False]
  -ek, --escape-key <escape-key>                  Set the escape key used to reach SerialTerm commands, eg. ^A, ^], 0x1D [default: ^A]
  -lk, --legacy-keys                              Also bind the original F1-F5, Home and ESC keys, ESC will not reach the device [default: False]
  -nu, --no-utf8                                  Leave the console output code page alone, for a device that sends its own 8 bit encoding rather than UTF-8
  -nh, --no-hint                                  Do not show the command hint line while the escape key is pending [default: False]
  -sl, --status-line                              Reserve the bottom row for a status line. Costs the device one row of screen [default: False]
  -l, --log <log>                                 Append everything the device sends to a file. Ctrl+A l starts and stops it during a session
  -lr, --log-raw                                  Keep ANSI escape sequences in the log instead of stripping them [default: False]
  -sf, --send-file <send-file>                    Send a text file to the device line by line after connecting. Ctrl+A s sends one during a session
  -sd, --send-delay <send-delay>                  Milliseconds to pause between lines when sending a file [default: 0]
  -sw, --send-wait <send-wait>                    Wait for this text from the device after each line, eg. the REPL prompt
  -st, --send-timeout <send-timeout>              How long to wait for --send-wait before giving up, in milliseconds [default: 2000]
  -m, --macro <macro>                             Bind text to Ctrl+A 1 through Ctrl+A 9, eg. --macro 1=reboot\r. Repeatable
  -le, --local-echo                               Show what you type. For devices that do not echo it back themselves [default: False]
  -ts, --timestamp <abs|off|rel>                  Prefix each line from the device with a time, abs is the clock and rel is seconds since connecting [default: off]
  -bs, --backspace <bs|del>                       Byte sent by the Backspace key, del is 0x7F and bs is 0x08 [default: del]
  -nl, --newline <cr|crlf|lf>                     Bytes sent by the Enter key [default: cr]
  -db, --data-bits <5|6|7|8>                      Sets the standard length of data bits per byte [default: 8]
  -pa, --parity <Even|Mark|None|Odd|Space>        Sets the parity-checking protocol [default: None]
  -sb, --stop-bits <One|OnePointFive|Two>         Sets the standard number of stopbits per byte [default: One]
  -hs, --handshake <None|RTS|RTSXonXoff|XonXoff>  Specifies the control protocol used in establishing a serial port communication [default: None]
  --version                                       Show version information
  -?, -h, --help                                  Show help and usage information

Commands:
  list      List all serial ports
  profiles  List saved argument profiles
```

## Exit codes

```
0  clean exit
1  the command line could not be parsed
2  no port was available or chosen
3  the device disconnected, with --disconnect-exit
4  the port rejected the settings
```

# Building

Needs the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```
dotnet build SerialTerm.sln
dotnet test SerialTerm.sln
```

A self contained single file for any supported platform:

```
dotnet publish SerialTerm/SerialTerm.csproj -c Release -r win-x64
dotnet publish SerialTerm/SerialTerm.csproj -c Release -r linux-x64
dotnet publish SerialTerm/SerialTerm.csproj -c Release -r osx-arm64
```

`win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64` and `osx-arm64`
are all supported. The Windows only parts - the console mode, the hint's screen
buffer reads, the device description lookup - are guarded at runtime, so the
same source builds everywhere.

Pushing a `v*` tag builds all six and opens a draft release with the binaries
attached.

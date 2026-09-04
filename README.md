# SerialTerm
A simple serial listener program for Windows Terminal command line replacement

Get a precompiled single file version 0.2.0 @ https://github.com/AdamKeher/SerialTerm/releases

SerialTerm needs the .NET 6 runtime @ https://dotnet.microsoft.com/download/dotnet/6.0

Get windows terminal @ https://github.com/microsoft/terminal

## About
SimpleTerm was created to provide a simple command line driven serial port listener with VT100 / ANSI support for use with my electronics projects.

### Features

* Uses the native VT100 / ANSI support provided in Windows Terminal
* Comprehensive list of command line options to control the serial port configuration
* Connection Management
  * Supports physical disconnection and reconnection of the serial port without exit 
  * Provides the ability to quickly close and open the serial port to allow access to the port from 3rd party flash tools
* Light weight implementation
* Soft reset Esp32 from SerialTerm during listening or on connection
* Soft reset Raspberry PI Pico
* Simple single file delployment
* Added support for NET6.0
* Full key pass through, so full screen programs such as `vi`, `nano` and `htop` work over the connection

## Terminal keys

SerialTerm sends every key to the connected device, including ESC, the function
keys, the arrows and Ctrl+C. Its own commands sit behind an escape key, `Ctrl+A`
by default: press the escape key, then a command key.

```
Ctrl+A ?          Display SerialTerm key help
Ctrl+A d          Disconnect / Reconnect serial connection
Ctrl+A i          Display serial port settings
Ctrl+A e          Soft reset ESP32 by toggling RTS enabled
Ctrl+A p          Reset PICO to programming mode by toggling 1200 baud connection
Ctrl+A l          Start / stop logging the session to a file
Ctrl+A v          Toggle hex view of incoming bytes
Ctrl+A f          Freeze / resume the screen, output keeps being captured
Ctrl+A b          Send a break to the device
Ctrl+A c          Clear terminal screen
Ctrl+A q          Exit terminal program
Ctrl+A Ctrl+A     Send a literal Ctrl+A to the connected device
```

Pressing the escape key paints a hint line along the bottom of the window
listing the commands, and puts back whatever was underneath it as soon as a
command key is pressed. It is drawn straight into the console buffer, so it
does not disturb the cursor, the colours, or anything a full screen program on
the device has drawn. Pass `--no-hint` to turn it off.

Use `--escape-key` to move it somewhere else, for example `--escape-key ^]` for
the telnet escape character. `--legacy-keys` additionally restores the original
F1 - F5, Home and ESC shortcuts, but with it ESC no longer reaches the device.

## Logging

`--log session.log` appends everything the device sends to a file. `Ctrl+A l`
starts and stops it mid session - with no `--log` given it picks a name from the
port and the time, so capture can be started the moment something interesting
happens.

ANSI escape sequences are stripped so the log stays readable and greppable;
`--log-raw` keeps them. The file is opened in append mode, so stopping and
restarting during a session adds to it rather than truncating.

## Break

`Ctrl+A b` holds the line in a break condition for 250 ms. A break is a run of
zero bits longer than a character frame, so no sequence of bytes can produce one -
it needs the driver. It interrupts U-Boot, reaches Linux SysRq, and drops some
bootloaders into command mode.

## Freeze

`Ctrl+A f` stops the screen moving so a stack trace can be read before the
device scrolls it away. The port stays open and the bytes keep being collected -
this is not `Ctrl+A d`, which closes the connection and loses whatever arrives.
Press it again to resume, and everything held is painted in order.

The buffer is capped at 1 MB. Past that the oldest bytes go and the resume
message says how many, since the reason to freeze is almost always to read
something that just happened.

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

## Backspace and Enter

Backspace sends DEL (`0x7F`), which is what readline, `nano`, `vi`, BusyBox ash
and the MicroPython REPL expect. Devices that want the older BS (`0x08`) instead
take `--backspace bs`.

Enter sends CR (`0x0D`). Use `--newline lf` or `--newline crlf` for devices that
want a line feed.

## Syntax
```
Description:
  SerialTerm - Simple serial port terminal program. (c)2021 AKsevenFour - https://github.com/AdamKeher/SerialTerm

Usage:
  SerialTerm [command] [options]

Options:
  -P, --port <port>                               Set the serial port to listen on
  -b, --baud <baud>                               Set serial port baud rate [default: 115200]
  -de, --disconnect-exit                          Exit terminal on disconnection [default: False]
  -r, --reset-esp32                               Reset ESP32 on connection [default: False]
  -dtr, --disable-dtr                             Disable DTR for serial connection [default: False]
  -rts, --disable-rts                             Disable RTS for serial connection [default: False]
  -ek, --escape-key <escape-key>                  Set the escape key used to reach SerialTerm commands, eg. ^A, ^], 0x1D [default: ^A]
  -lk, --legacy-keys                              Also bind the original F1-F5, Home and ESC keys, ESC will not reach the device [default: False]
  -nh, --no-hint                                  Do not show the command hint line while the escape key is pending [default: False]
  -bs, --backspace <bs|del>                       Byte sent by the Backspace key, del is 0x7F and bs is 0x08 [default: del]
  -nl, --newline <cr|crlf|lf>                     Bytes sent by the Enter key [default: cr]
  -l, --log <log>                                 Append everything the device sends to a file. Ctrl+A l starts and stops it during a session
  -lr, --log-raw                                  Keep ANSI escape sequences in the log instead of stripping them [default: False]
  -ts, --timestamp <abs|off|rel>                  Prefix each line from the device with a time, abs is the clock and rel is seconds since connecting [default: off]
  -db, --data-bits <5|6|7|8>                      Sets the standard length of data bits per byte [default: 8]
  -pa, --parity <Even|Mark|None|Odd|Space>        Sets the parity-checking protocol [default: None]
  -sb, --stop-bits <One|OnePointFive|Two>         Sets the standard number of stopbits per byte [default: One]
  -hs, --handshake <None|RTS|RTSXonXoff|XonXoff>  Specifies the control protocol used in establishing a serial port communication [default: None]
  --version                                       Show version information
  -?, -h, --help                                  Show help and usage information

Commands:
  list  List all serial ports
 ```

### list

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

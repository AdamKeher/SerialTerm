# SerialTerm
A simple serial listener program for Windows Terminal command line replacement

Get a precompiled single file version 0.1.1 @ https://github.com/AdamKeher/SerialTerm/releases

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
* Simple single file delployment

## Syntax
```
SerialTerm:
  SerialTerm - Simple serial port terminal program. (c)2021 AKsevenFour - https://github.com/AdamKeher/SerialTerm

Usage:
  SerialTerm [options] [command]

Options:
  -P, --port <port>                                 Set the serial port to listen on
  -b, --baud <baud>                                 Set serial port baud rate [default: 115200]
  -de, --disconnect-exit                            Exit terminal on disconnection [default: False]
  -db, --data-bits <5|6|7|8>                        Sets the standard length of data bits per byte [default: 8]
  -pa, --parity <Even|Mark|None|Odd|Space>          Sets the parity-checking protocol [default: None]
  -sb, --stop-bits <One|OnePointFive|Two>           Sets the standard number of stopbits per byte [default: One]
  -hs, --handshake <None|RTS|RTSXonXoff|XonXoff>    Specifies the control protocol used in establishing a serial port communication [default: None]
  --version                                         Show version information
  -?, -h, --help                                    Show help and usage information

Commands:
  list    List all serial ports
 ```

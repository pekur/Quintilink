# Quintilink

A modern WPF-based TCP/Serial communication tester for Windows, built with .NET 10.

![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![License](https://img.shields.io/badge/License-Non--Commercial-orange)

> ?? **License Notice**: This software is free for personal and non-commercial use. Commercial use requires a paid license. See [LICENSE](LICENSE) for details.

## Features

### Connection Modes
- **TCP Client** - Connect to remote TCP servers
- **TCP Server** - Host a TCP server and accept multiple client connections
- **Serial Port** - Communicate via COM ports with configurable baud rate, parity, data bits, and stop bits

### Messaging
- **Predefined Messages** - Save frequently used messages for quick sending
- **Quick Send** - Send messages on-the-fly with mixed ASCII/hex input
- **Auto Responses** - Configure automatic reactions to specific trigger patterns
  - Multiple responses per trigger supported
  - Individual pause/resume control for each response
  - Configurable response delay

### Input Formats
- Plain ASCII text
- Hex bytes in angle brackets: `HELLO<0D 0A>`
- Macro shortcuts for control characters:
  - `<CR>` - Carriage Return (0x0D)
  - `<LF>` - Line Feed (0x0A)
  - `<TAB>` - Tab (0x09)
  - `<NUL>` - Null (0x00)
  - `<ESC>` - Escape (0x1B)
  - `<ACK>`, `<NAK>`, `<STX>`, `<ETX>`, and more ASCII control codes

### Logging & Analysis
- **Real-time Log** - View all sent/received data with timestamps
- **Bookmarks** - Mark important log entries for easy reference (click the gutter to toggle)
- **Search** - Find patterns in log with regex support and direction filtering
- **Export** - Save logs to CSV, JSON, or plain text formats
- **Message Comparison** - Compare two predefined messages byte-by-byte

### Statistics
- Bytes sent/received counters
- Connection duration tracking
- Real-time statistics window

## Screenshots

*Coming soon*

## Installation

### Requirements
- Windows 10/11
- .NET 10 Runtime

### Download
Download the latest release from the [Releases](https://github.com/pekur/Quintilink/releases) page.

### Build from Source
```bash
git clone https://github.com/pekur/Quintilink.git
cd Quintilink
dotnet build
```

## Usage

### Quick Start

1. **Select Connection Mode** - Choose TCP Client, TCP Server, or Serial Port
2. **Configure Connection**
   - TCP Client: Enter host and port
   - TCP Server: Enter port to listen on
   - Serial Port: Select COM port and configure parameters
3. **Connect** - Click the Connect button
4. **Send Data** - Use Quick Send or select a predefined message

### Quick Send Examples

```
Hello World                    # Plain ASCII
Hello<0D 0A>                   # ASCII with hex line ending
<02>DATA<03>                   # STX + DATA + ETX framing
GET / HTTP/1.1<CR><LF><CR><LF> # HTTP request with macros
```

### Auto Responses

Auto responses allow automatic replies when specific hex patterns are received:

1. Go to the **Auto responses** tab
2. Click **Add** to create a new response
3. Enter the **Trigger** pattern (hex format)
4. Enter the **Response** data
5. Optionally set a **Delay** before responding
6. Use the **Pause/Play** button to enable/disable individual responses

Multiple responses can share the same trigger - all enabled responses will be sent when triggered.

## Data Storage

Application data is stored in the application directory:

- `store.json` - Predefined messages and auto responses
- `settings.json` - Connection settings and preferences

## Building

### Prerequisites
- Visual Studio 2022 or later
- .NET 10 SDK

### Build Commands
```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Publish self-contained executable
dotnet publish -c Release -r win-x64 --self-contained
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project uses a **Non-Commercial License**:

- ? **Free** for personal, educational, and evaluation use
- ? **Free** to modify and share for non-commercial purposes
- ? **Commercial use requires a paid license**

For commercial licensing inquiries, please [open an issue](https://github.com/pekur/Quintilink/issues) on GitHub.

See the [LICENSE](LICENSE) file for full details.

## Acknowledgments

- Built with [WPF UI](https://github.com/lepoco/wpfui) for modern Windows 11 styling
- Uses [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) for MVVM patterns

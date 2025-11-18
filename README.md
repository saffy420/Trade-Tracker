# Trade Tracker - Cross-Platform Edition

A fully cross-platform desktop application for tracking trades, built with Avalonia UI and .NET 8.

## Features

- **Cross-Platform**: Runs on Windows, macOS, and Linux
- **Modern UI**: Dark-themed, responsive interface with MVVM architecture
- **Automated Macros**: F1, F2, F3 hotkeys for automated trade tracking workflows
- **OCR Integration**: Tesseract OCR for reading screen text
- **Configurable Coordinates**: Edit all macro coordinates via built-in editor
- **Live Reload**: Configuration changes apply without restart
- **Trade Management**: Group trades by game, filter, and delete as needed

## Requirements

### All Platforms
- .NET 8.0 SDK or Runtime
- Tesseract OCR (included in build)

### Linux-Specific
- `xdotool` for keyboard/mouse automation: `sudo apt install xdotool`
- `scrot` or `imagemagick` for screen capture: `sudo apt install scrot`

### macOS
- macOS 10.15 or later
- Accessibility permissions may be required for automation

## Building from Source

```bash
# Clone or extract the project
cd Tracker.Avalonia

# Restore dependencies
dotnet restore

# Build
dotnet build -c Release

# Run
dotnet run
```

## Publishing

### Windows (Self-Contained)
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### macOS (Self-Contained)
```bash
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

### Linux (Self-Contained)
```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

The output will be in `bin/Release/net8.0/{runtime}/publish/`

## Usage

### Keyboard Shortcuts
- **F1**: Save trade (triple-click game name and trade type, copy, save)
- **F2**: Run macro (automated trade entry workflow)
- **F3**: Abort F2 macro and cleanup
- **Escape**: Clear active filter and show all trades

### Coordinate Editor
Click the **Settings** icon (⚙️) in the title bar to open the Coordinate Editor where you can:
- View all macro coordinates
- Edit X/Y values for click positions
- Add new coordinate entries
- Delete unused coordinates
- Test coordinates (displays value)
- Save changes (live reload, no restart needed)

### Trade Management
- Right-click any trade to **Delete** it
- Trades are grouped by game automatically
- Filter is applied during F2 macro and cleared by F3 or Escape

## Configuration

### Trades File Location
- **Windows**: `%APPDATA%\TradeTracker\trades.txt`
- **macOS/Linux**: `~/.config/TradeTracker/trades.txt` or `~/.local/share/TradeTracker/trades.txt`

The app automatically migrates from the old location (`Documents/trades.txt`) on first run.

### Coordinates Configuration
Edit via the built-in Coordinate Editor or directly:
- File: `Config/coordinates.json` (next to executable)
- Changes are detected automatically and reloaded

### Logs
Application logs are saved to: `logs/tracker-YYYYMMDD.log`

## Troubleshooting

### Tesseract OCR Errors
Ensure the `tessdata` folder exists next to the executable with `eng.traineddata` inside.

### Linux Automation Not Working
Install required tools:
```bash
sudo apt install xdotool scrot
```

### macOS Automation Permissions
Grant accessibility permissions in System Preferences → Security & Privacy → Privacy → Accessibility

## Architecture

### MVVM Structure
- **Models**: `Trade`, `TradeGroup`, `CoordinateConfig`
- **ViewModels**: `MainWindowViewModel`, `CoordinateEditorViewModel`
- **Views**: `MainWindow`, `CoordinateEditorView`
- **Services**: Abstracted platform-specific functionality

### Services
- `TradeStorageService`: File I/O for trades
- `CoordinateConfigService`: Config management with live reload
- `MacroService`: F1/F2/F3 macro orchestration
- `OcrService`: Tesseract wrapper
- `IKeyboardService`, `IMacroClickService`, `IScreenCaptureService`: Platform-specific implementations

## License

This project is provided as-is for personal use.

## Credits

Built with:
- [Avalonia UI](https://avaloniaui.net/)
- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)


# Trade Tracker

A cross-platform desktop application for tracking and placing trades, built with Avalonia UI and .NET 8.

## Why I Built This

This started with wanting to make my own life easier. A +EV trade would pop up on my dashboard, I would click it, evaluate it, then go over to my second monitor and have to enter in the right numbers and place the trade. It was slow and repetitive, and it was costing me time and opportunity. With this tool, I could just press a button and have the trade placed for me within a second. It took my focus down from four things at once to just one.

What started as a one-day project that barely worked ended up turning into a one-month project. I wanted it to run on my Mac Minis and my Windows desktop, so it needed to be cross-platform. That decision is what made it a real project.

## What I Learned

Going cross-platform was harder than I expected. I had to learn how to write platform-specific code (keyboard input, screen capture, mouse automation) behind shared interfaces so the same core logic could run on Windows, macOS, and Linux without rewriting everything three times.

I also learned MVVM properly for the first time. Before this I was just shoving all the logic into code-behind files, which gets messy fast. Using the MVVM pattern here made the code a lot easier to follow and change.

OCR was something I had never touched before. Getting Tesseract to reliably read small text off a screen required a lot of preprocessing tweaking. I spent more time on that than I expected.

The biggest takeaway was probably how much a small project can grow once you start actually using it. It works fine for one platform, then you want to use it on another machine, then another, and suddenly you're rewriting the whole input layer.

## Features

- **Cross-Platform**: Runs on Windows, macOS, and Linux
- **Automated Macros**: F1, F2, F3 hotkeys for automated trade tracking workflows
- **OCR Integration**: Tesseract OCR for reading screen text
- **Configurable Coordinates**: Edit all macro coordinates via a built-in editor
- **Live Reload**: Configuration changes apply without restart
- **Trade Management**: Trades are grouped by game, with filtering and deletion

## Requirements

### All Platforms
- .NET 8.0 SDK or Runtime
- Tesseract OCR (included in build)

### Linux
- `xdotool` for keyboard/mouse automation: `sudo apt install xdotool`
- `scrot` or `imagemagick` for screen capture: `sudo apt install scrot`

### macOS
- macOS 10.15 or later
- Accessibility permissions may be required for automation

## Building from Source

```bash
cd Tracker.Avalonia

dotnet restore

dotnet build -c Release

dotnet run
```

## Publishing

### Windows
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### macOS
```bash
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

### Linux
```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

Output goes to `bin/Release/net8.0/{runtime}/publish/`

## Usage

### Keyboard Shortcuts
- **F1**: Save trade (triple-click game name and trade type, copy, save)
- **F2**: Run macro (automated trade entry workflow)
- **F3**: Abort F2 macro and cleanup
- **Escape**: Clear active filter and show all trades

### Coordinate Editor
Click the Settings icon in the title bar to open the Coordinate Editor where you can:
- View all macro coordinates
- Edit X/Y values for click positions
- Add new coordinate entries
- Delete unused coordinates
- Save changes (live reload, no restart needed)

### Trade Management
- Right-click any trade to delete it
- Trades are grouped by game automatically
- Filter is applied during F2 macro and cleared by F3 or Escape

## Configuration

### Trades File Location
- **Windows**: `%APPDATA%\TradeTracker\trades.txt`
- **macOS/Linux**: `~/.config/TradeTracker/trades.txt`

The app automatically migrates from the old location (`Documents/trades.txt`) on first run.

### Coordinates
Edit via the built-in Coordinate Editor or directly in `Config/coordinates.json` (next to the executable). Changes are detected automatically and reloaded.

### Logs
Application logs are saved to `logs/tracker-YYYYMMDD.log`

## Troubleshooting

### Tesseract OCR Errors
Make sure the `tessdata` folder exists next to the executable with `eng.traineddata` inside.

### Linux Automation Not Working
```bash
sudo apt install xdotool scrot
```

### macOS Accessibility Permissions
Grant accessibility permissions in System Preferences > Security and Privacy > Privacy > Accessibility.

## Architecture

- **Models**: `Trade`, `TradeGroup`, `CoordinateConfig`
- **ViewModels**: `MainWindowViewModel`, `CoordinateEditorViewModel`
- **Views**: `MainWindow`, `CoordinateEditorView`
- **Services**: `TradeStorageService`, `CoordinateConfigService`, `MacroService`, `OcrService`, and platform-specific input implementations

## Credits

- [Avalonia UI](https://avaloniaui.net/)
- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)

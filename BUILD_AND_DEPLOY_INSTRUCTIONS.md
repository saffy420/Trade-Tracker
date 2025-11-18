# STEP 12: BUILD, PUBLISH, AND DEPLOYMENT INSTRUCTIONS

## Prerequisites

### All Platforms
- .NET 8.0 SDK installed ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Tesseract traineddata file (`eng.traineddata`) in `tessdata/` folder

### Platform-Specific Requirements

#### Windows
- Visual Studio 2022 (optional, for development)
- No additional dependencies needed

#### macOS
- macOS 10.15 (Catalina) or later
- Xcode Command Line Tools: `xcode-select --install`

#### Linux
- Ubuntu/Debian: `sudo apt install xdotool scrot`
- Fedora/RHEL: `sudo dnf install xdotool scrot`
- Arch: `sudo pacman -S xdotool scrot`

---

## Building from Source

### 1. Clone/Extract Project
```bash
cd Tracker.Avalonia
```

### 2. Restore Dependencies
```bash
dotnet restore
```

### 3. Build Debug Version
```bash
dotnet build -c Debug
```

### 4. Build Release Version
```bash
dotnet build -c Release
```

### 5. Run Locally
```bash
dotnet run
```

---

## Publishing for Distribution

### Windows

#### Option 1: Self-Contained Single File (Recommended)
```bash
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

Output: `bin/Release/net8.0/win-x64/publish/Tracker.Avalonia.exe`

#### Option 2: Framework-Dependent (Smaller Size)
```bash
dotnet publish -c Release -r win-x64 \
  --self-contained false \
  -p:PublishSingleFile=true
```

Requires .NET 8 Runtime installed on target machine.

#### Option 3: Portable (All Files)
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

Output folder: `bin/Release/net8.0/win-x64/publish/`

### macOS

#### Option 1: Self-Contained Single File
```bash
dotnet publish -c Release -r osx-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

For Apple Silicon (M1/M2):
```bash
dotnet publish -c Release -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `bin/Release/net8.0/osx-x64/publish/Tracker.Avalonia`

#### Option 2: Create .app Bundle
```bash
# Publish first
dotnet publish -c Release -r osx-x64 --self-contained true

# Create .app structure
mkdir -p TradeTracker.app/Contents/MacOS
mkdir -p TradeTracker.app/Contents/Resources

# Copy executable
cp -r bin/Release/net8.0/osx-x64/publish/* TradeTracker.app/Contents/MacOS/

# Create Info.plist
cat > TradeTracker.app/Contents/Info.plist << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>Tracker.Avalonia</string>
    <key>CFBundleIconFile</key>
    <string>icon.icns</string>
    <key>CFBundleIdentifier</key>
    <string>com.tradetracker.avalonia</string>
    <key>CFBundleName</key>
    <string>Trade Tracker</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOF

# Make executable
chmod +x TradeTracker.app/Contents/MacOS/Tracker.Avalonia
```

### Linux

#### Option 1: Self-Contained
```bash
dotnet publish -c Release -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `bin/Release/net8.0/linux-x64/publish/Tracker.Avalonia`

#### Option 2: AppImage (Portable)
```bash
# Install appimagetool
wget https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x appimagetool-x86_64.AppImage

# Publish
dotnet publish -c Release -r linux-x64 --self-contained true

# Create AppDir structure
mkdir -p TradeTracker.AppDir/usr/bin
mkdir -p TradeTracker.AppDir/usr/share/applications
mkdir -p TradeTracker.AppDir/usr/share/icons/hicolor/256x256/apps

# Copy files
cp -r bin/Release/net8.0/linux-x64/publish/* TradeTracker.AppDir/usr/bin/

# Create desktop file
cat > TradeTracker.AppDir/usr/share/applications/tradetracker.desktop << EOF
[Desktop Entry]
Type=Application
Name=Trade Tracker
Exec=Tracker.Avalonia
Icon=tradetracker
Categories=Utility;
EOF

# Create AppRun
cat > TradeTracker.AppDir/AppRun << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE=${SELF%/*}
export PATH="${HERE}/usr/bin/:${PATH}"
export LD_LIBRARY_PATH="${HERE}/usr/lib/:${LD_LIBRARY_PATH}"
cd "${HERE}/usr/bin"
exec "${HERE}/usr/bin/Tracker.Avalonia" "$@"
EOF
chmod +x TradeTracker.AppDir/AppRun

# Build AppImage
./appimagetool-x86_64.AppImage TradeTracker.AppDir TradeTracker.AppImage
```

---

## Packaging Tesseract and tessdata

### Automatic (Included in Build)
The `.csproj` file is configured to copy the `tessdata/` folder automatically:
```xml
<ItemGroup>
  <None Include="tessdata\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Manual Setup
1. Download `eng.traineddata` from [Tesseract GitHub](https://github.com/tesseract-ocr/tessdata)
2. Place in project folder: `Tracker.Avalonia/tessdata/eng.traineddata`
3. Rebuild or republish

### Verification
After publishing, verify the structure:
```
publish/
├── Tracker.Avalonia(.exe)
├── tessdata/
│   └── eng.traineddata
└── Config/
    └── coordinates.json
```

---

## Platform-Specific Native Dependencies

### Windows
- Native Tesseract binaries included via NuGet package
- Located in: `x64/`, `x86/` folders
- No manual action needed

### macOS
- Tesseract binaries included in NuGet package
- May need to grant permissions:
  - System Preferences → Security & Privacy → Privacy → Accessibility
  - Add Trade Tracker to allowed apps

### Linux
- Tesseract native library (libtesseract.so) included in NuGet
- Install system dependencies:
```bash
sudo apt install libtesseract5 xdotool scrot
```

---

## Deployment Packaging

### Windows - Installer (WiX Toolset)
```bash
# Install WiX
dotnet tool install --global wix

# Create .msi installer
# (Requires .wxs file configuration - see WiX documentation)
```

### Windows - Portable ZIP
```bash
# After publishing
cd bin/Release/net8.0/win-x64/publish
zip -r TradeTracker-Windows-x64.zip .
```

### macOS - DMG
```bash
# Create DMG from .app bundle
hdiutil create -volname "Trade Tracker" -srcfolder TradeTracker.app -ov -format UDZO TradeTracker.dmg
```

### Linux - .deb Package
```bash
# Create package structure
mkdir -p tradetracker_1.0.0/DEBIAN
mkdir -p tradetracker_1.0.0/usr/bin
mkdir -p tradetracker_1.0.0/usr/share/applications
mkdir -p tradetracker_1.0.0/usr/share/icons

# Copy files
cp -r bin/Release/net8.0/linux-x64/publish/* tradetracker_1.0.0/usr/bin/

# Create control file
cat > tradetracker_1.0.0/DEBIAN/control << EOF
Package: tradetracker
Version: 1.0.0
Architecture: amd64
Maintainer: Your Name <your@email.com>
Description: Cross-platform trade tracking application
Depends: libtesseract5, xdotool, scrot
EOF

# Build .deb
dpkg-deb --build tradetracker_1.0.0
```

### Linux - .tar.gz
```bash
cd bin/Release/net8.0/linux-x64/publish
tar -czf TradeTracker-Linux-x64.tar.gz *
```

---

## Runtime Configuration

### First Run
The application will:
1. Create config folder in AppData (Windows) or ~/.config (Linux/Mac)
2. Migrate existing trades from Documents/trades.txt if found
3. Create default coordinates.json if missing
4. Initialize tessdata folder check

### Configuration Locations

#### Windows
- Trades: `%APPDATA%\TradeTracker\trades.txt`
- Logs: `{AppDir}\logs\`
- Config: `{AppDir}\Config\coordinates.json`

#### macOS
- Trades: `~/.config/TradeTracker/trades.txt`
- Logs: `{AppDir}/logs/`
- Config: `{AppDir}/Config/coordinates.json`

#### Linux
- Trades: `~/.config/TradeTracker/trades.txt` or `~/.local/share/TradeTracker/trades.txt`
- Logs: `{AppDir}/logs/`
- Config: `{AppDir}/Config/coordinates.json`

---

## Testing Checklist

### Functional Tests
- [ ] Application launches successfully
- [ ] Main window displays correctly
- [ ] Trades load and display
- [ ] F1 macro executes (if screen positions are configured)
- [ ] F2 macro executes
- [ ] F3 macro executes and aborts F2
- [ ] Escape clears filter
- [ ] Right-click delete works
- [ ] Coordinate editor opens
- [ ] Coordinate editor saves changes
- [ ] OCR recognizes text (test with sample screenshot)

### Cross-Platform Tests
- [ ] Windows 10/11
- [ ] macOS 10.15+
- [ ] Ubuntu 20.04+
- [ ] Other Linux distributions

### Integration Tests
- [ ] Tesseract OCR loads successfully
- [ ] Screen capture works
- [ ] Keyboard automation works (with permissions)
- [ ] Mouse automation works (with permissions)
- [ ] File I/O works (trades.txt read/write)
- [ ] Config hot-reload works

---

## Troubleshooting Deployment

### "tessdata not found" Error
- Verify `tessdata/eng.traineddata` exists in publish folder
- Check .csproj `<None Include>` configuration
- Manually copy if needed

### "libtesseract not found" (Linux)
```bash
sudo apt install libtesseract5
# OR
export LD_LIBRARY_PATH=/path/to/tesseract/lib:$LD_LIBRARY_PATH
```

### "Permission Denied" (macOS/Linux)
```bash
chmod +x Tracker.Avalonia
```

### Automation Not Working (macOS)
- Grant Accessibility permissions
- System Preferences → Security & Privacy → Privacy → Accessibility
- Add application to allowed list

### Automation Not Working (Linux)
```bash
# Install required tools
sudo apt install xdotool scrot

# Verify X11 is running (not Wayland)
echo $XDG_SESSION_TYPE
```

### Single File Publish Too Large
Use framework-dependent publish to reduce size:
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## Performance Optimization

### Reduce Publish Size
```bash
# Enable ReadyToRun compilation
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishReadyToRun=true \
  -p:PublishTrimmed=false

# Note: Trimming may break reflection-based features
```

### Startup Performance
- Use self-contained publish for faster startup
- Consider AOT compilation for .NET 9+

---

## Distribution Checklist

Before releasing:
- [ ] Version number updated in .csproj
- [ ] README.md updated with version info
- [ ] CHANGELOG.md created (optional)
- [ ] All tests passed
- [ ] Tested on all target platforms
- [ ] License file included (if applicable)
- [ ] Icon added to executable
- [ ] Digital signature applied (Windows/macOS)
- [ ] Notarization completed (macOS)

---

## Support and Maintenance

### Updating Coordinates
Users can edit coordinates via:
1. Built-in Coordinate Editor UI
2. Direct editing of `Config/coordinates.json`

Changes reload automatically without restart.

### Updating Trades
Users can:
1. Add trades via F1 macro
2. Delete trades via right-click
3. Manually edit `trades.txt` (two lines per trade: game, market)

### Logs
View application logs at `logs/tracker-YYYYMMDD.log` for debugging.

---

## Additional Resources

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [.NET Publishing Documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract)
- [xdotool Documentation](https://www.semicomplete.com/projects/xdotool/)

---

**End of Build and Deployment Instructions**


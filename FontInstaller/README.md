# Font Installer for Windows 10/11

A .NET console application that automatically installs fonts on Windows 10/11 systems. The application copies font files to the system Fonts directory and adds the necessary registry entries to make them available system-wide.

## Features

- Automatically installs TTF, OTF, and TTC font files
- Copies fonts to the Windows Fonts directory
- Adds registry entries for proper font recognition
- Notifies Windows of font changes
- Requires administrator privileges for installation
- Published as a single executable file
- Supports USB token certificate signing
- **NEW**: Embeds fonts directly in the executable for portable installation

## Usage

### Command Line Options

```bash
# Install embedded fonts to the system Fonts directory (default behavior)
fontinstaller.exe

# Install embedded fonts to a custom destination
fontinstaller.exe --embedded "C:\Custom\Fonts\Path"

# Install fonts from a specific directory to the system Fonts directory
fontinstaller.exe [source_directory]

# Install fonts from a specific directory to a custom destination
fontinstaller.exe [source_directory] [destination_directory]
```

### Examples

```bash
# Install embedded fonts (default behavior)
fontinstaller.exe

# Install fonts from a specific folder
fontinstaller.exe "C:\MyFonts"

# Install fonts to a custom destination
fontinstaller.exe "C:\MyFonts" "C:\Windows\Fonts"

# Install embedded fonts to a custom destination
fontinstaller.exe --embedded "C:\MyCustomFontDir"
```

## Building the Project

### Prerequisites

- .NET 8 SDK
- Windows SDK (for code signing with signtool on Windows)
- Xcode Command Line Tools (for code signing on macOS)

### Build Scripts

The project includes build scripts for multiple platforms:

#### Windows
```cmd
cd FontInstaller/FontInstaller.Console
.\build-win-x64.ps1
```

#### macOS
```bash
cd FontInstaller/FontInstaller.Console
# For Apple Silicon (M1/M2/M3/M4)
./build-osx-arm64.sh

# For Intel Macs
./build-osx-x64.sh

# Or use the universal script
./build.sh osx-arm64
./build.sh osx-x64
```

#### Universal Build Script
```bash
cd FontInstaller/FontInstaller.Console
# Auto-detect platform
./build.sh

# Or specify platform
./build.sh osx-arm64
./build.sh osx-x64
./build.sh win-x64

# Build all platforms (macOS only)
./build.sh all
```

The build scripts will:
- Publish the application as a single executable
- Embed all fonts from the Fonts/ directory into the executable
- Attempt to sign the executable with a code signing certificate (if available)
- Place the final executable in the `dist` folder

## Embedded Fonts

The application now supports embedding fonts directly into the executable:
- All font files in the `Fonts/` directory are automatically embedded
- The `--embedded` flag installs these embedded fonts
- This creates a completely portable font installer with no external dependencies
- Fonts are extracted temporarily during installation and cleaned up afterward

## Code Signing

The build scripts include automatic code signing functionality:

### Windows
- Automatically detects code signing certificates in the system
- Supports USB token certificates
- Signs with SHA256 and timestamps the executable
- Falls back to unsigned if no certificate is found

### macOS
- Uses `codesign` utility for signing
- Supports Apple Developer ID certificates
- Includes runtime enforcement for .NET applications
- Can use entitlements.plist for special permissions

To prepare for code signing:
1. Install your code signing certificate in the appropriate system (Windows Certificate Store or macOS Keychain)
2. For USB tokens, ensure the device is connected during the build process
3. Have your PIN ready if required by the certificate
4. On macOS, ensure your Apple Developer ID is configured in Keychain Access

## Security Notes

- The application requires administrator privileges to install fonts system-wide
- Always verify the source of fonts before installation
- Code signing helps verify the authenticity of the executable

## License

MIT License - See LICENSE file for details.
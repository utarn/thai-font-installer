# Font Installer Application

A cross-platform .NET console application that automates font installation on Windows, macOS, and Linux systems. The application copies font files to the system fonts directory and handles necessary registry entries or system notifications to make them available system-wide.

## 🚀 Purpose

The Font Installer is designed to simplify the process of installing fonts across different operating systems. It addresses the common challenge of deploying fonts in enterprise environments or distributing fonts with applications by providing a single executable that can install fonts programmatically.

### Key Features
- **Cross-platform support**: Works on Windows 10/11, macOS, and Linux
- **Multiple installation methods**: Install from directory or from embedded fonts
- **Administrator privilege handling**: Checks for required permissions
- **Registry integration**: Properly registers fonts on Windows systems
- **System notification**: Notifies the OS of font changes
- **Portable distribution**: Can embed fonts directly in the executable
- **Code signing support**: Includes scripts for signing executables

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Windows SDK (for code signing with signtool on Windows)
- Xcode Command Line Tools (for code signing on macOS)

## 🛠️ How to Build

### Quick Build
```bash
# Navigate to the console application directory
cd FontInstaller/FontInstaller.Console

# Run the universal build script
./build.sh
```

### Platform-Specific Builds

#### Windows
```powershell
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

# Or use the universal script with specific targets
./build.sh osx-arm64
./build.sh osx-x64
```

#### All Platforms (from macOS)
```bash
cd FontInstaller/FontInstaller.Console
./build.sh all
```

The build scripts will:
- Publish the application as a single executable
- Embed all fonts from the `Fonts/` directory into the executable
- Attempt to sign the executable with a code signing certificate (if available)
- Place the final executable in the `dist` folder

## 📖 How to Use

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

> ⚠️ **Note**: The application requires administrator privileges for system-wide font installation. On Windows, run as Administrator. On macOS/Linux, run with `sudo`.

## 🧱 Architecture Overview

### Components

- **FontInstallerService**: Core implementation of font installation logic
- **IFontInstaller**: Interface defining font installation operations
- **Program**: Entry point that handles command-line arguments and orchestrates installation

### Cross-Platform Support

The application handles platform differences:
- **Windows**: Uses registry entries and GDI+ API for font registration
- **macOS**: Places fonts in `/Library/Fonts/` or `~/Library/Fonts/`
- **Linux**: Places fonts in `/usr/share/fonts/` or `~/.local/share/fonts/` and refreshes font cache

### Embedded Fonts

The application supports embedding fonts directly into the executable:
- All font files in the `Fonts/` directory are automatically embedded during build
- The `--embedded` flag installs these embedded fonts
- This creates a completely portable font installer with no external dependencies
- Fonts are extracted temporarily during installation and cleaned up afterward

## 🔐 Security Notes

- The application requires administrator privileges to install fonts system-wide
- Always verify the source of fonts before installation
- Code signing helps verify the authenticity of the executable
- The application checks for administrator rights before attempting installation

## 🤝 Contributing

We welcome contributions to improve the Font Installer! Here's how you can help:

### Setting Up for Development

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/font-installer.git
   cd font-installer
   ```

2. Install the .NET 8 SDK

3. Open the solution in Visual Studio, Visual Studio Code, or your preferred IDE:
   ```bash
   dotnet sln add FontInstaller/FontInstaller.Console/FontInstaller.Console.csproj
   ```

4. Build the project:
   ```bash
   cd FontInstaller/FontInstaller.Console
   dotnet build
   ```

### Adding New Features

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add tests if applicable
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Testing

Run the unit tests:
```bash
cd FontInstaller/FontInstaller.Tests
dotnet test
```

### Code Style

- Follow .NET/C# coding conventions
- Write clear, descriptive commit messages
- Include documentation for public APIs
- Add unit tests for new functionality

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔄 Version History

- **v1.0.0**: Initial release with Windows support
- **v1.1.0**: Added macOS and Linux support
- **v1.2.0**: Added embedded fonts capability
- **v1.3.0**: Improved cross-platform compatibility and code signing support

## 🆘 Support

If you encounter issues or have questions:
1. Check the existing issues in the repository
2. Search for similar problems in the documentation
3. Open a new issue with detailed information about your problem
4. Include your operating system, .NET version, and steps to reproduce

---

Made with ❤️ for developers who want to simplify font deployment across platforms.
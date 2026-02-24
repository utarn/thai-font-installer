using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace FontInstaller.ConsoleApp // Changed namespace to avoid conflict with System.Console
{
    public interface IFontInstaller
    {
        bool IsAdministrator();
        void InstallFonts(string sourceDirectory, string destinationDirectory);
        void InstallEmbeddedFonts(string destinationDirectory);
        void InstallSingleFont(string fontFilePath, string destinationDirectory);
        void AddFontToRegistry(string fontFileName);
        string GetFontNameFromFileName(string fileName);
        void NotifyFontChange();
        string GetSystemFontsDirectory();
    }

    public class FontInstallerService : IFontInstaller
    {
        [DllImport("gdi32.dll")]
        private static extern int AddFontResource(string lpszFilename);

        [DllImport("gdi32.dll")]
        private static extern bool RemoveFontResource(string lpszFilename);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_FONTCHANGE = 0x001D;
        private const int HWND_BROADCAST = 0xffff;

        public bool IsAdministrator()
        {
            try
            {
                // Check if running on Windows first
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    WindowsIdentity identity = WindowsIdentity.GetCurrent();
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
                else
                {
                    // On Unix-like systems (macOS, Linux), check if running as root
                    Process process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "id",
                            Arguments = "-u",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    int userId = int.Parse(output.Trim());
                    return userId == 0; // Root user ID is 0
                }
            }
            catch
            {
                return false;
            }
        }

        public string GetSystemFontsDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // On macOS, fonts can be installed to different locations:
                // ~/Library/Fonts/ (user fonts) or /Library/Fonts/ (system fonts)
                // For system-wide installation, we'll use /Library/Fonts/ which requires admin
                return "/Library/Fonts/";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // On Linux, fonts are typically in /usr/share/fonts/ or ~/.local/share/fonts/
                // For system-wide installation, we'll use /usr/share/fonts/
                return "/usr/share/fonts/";
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported operating system for font installation.");
            }
        }

        public void InstallFonts(string sourceDirectory, string destinationDirectory)
        {
            var supportedExtensions = new[] { ".ttf", ".otf", ".ttc" };
            var fontFiles = Directory.GetFiles(sourceDirectory)
                .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                .ToArray();

            if (fontFiles.Length == 0)
            {
                throw new InvalidOperationException("No font files found in the source directory.");
            }

            foreach (var fontFile in fontFiles)
            {
                InstallSingleFont(fontFile, destinationDirectory);
            }

            // Notify system of font changes
            NotifyFontChange();
        }

        public void InstallEmbeddedFonts(string destinationDirectory)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            
            var supportedExtensions = new[] { ".ttf", ".otf", ".ttc" };
            var embeddedFontResources = resourceNames
                .Where(resourceName => supportedExtensions.Any(ext => resourceName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            if (embeddedFontResources.Length == 0)
            {
                throw new InvalidOperationException("No embedded font resources found in the application.");
            }

            // Create a temporary directory to extract embedded fonts
            string tempDir = Path.Combine(Path.GetTempPath(), "FontInstaller_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                foreach (var resourceName in embeddedFontResources)
                {
                    // Extract the font file to the temporary directory
                    string fileName = Path.GetFileName(resourceName.Replace("Fonts/", "").Replace("Fonts\\", ""));
                    string tempFontPath = Path.Combine(tempDir, fileName);

                    Stream? stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using (stream)
                        {
                            using (FileStream fileStream = new FileStream(tempFontPath, FileMode.Create))
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                    }

                    // Install the extracted font
                    if (File.Exists(tempFontPath))
                    {
                        InstallSingleFont(tempFontPath, destinationDirectory);
                    }
                }

                // Notify system of font changes
                NotifyFontChange();
            }
            finally
            {
                // Clean up the temporary directory
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // If we can't delete the temp directory, just ignore
                    // This might happen if files are locked by the system
                }
            }
        }

        public void InstallSingleFont(string fontFilePath, string destinationDirectory)
        {
            if (!File.Exists(fontFilePath))
            {
                throw new FileNotFoundException($"Font file does not exist: {fontFilePath}");
            }

            if (!Directory.Exists(destinationDirectory))
            {
                throw new DirectoryNotFoundException($"Destination directory does not exist: {destinationDirectory}");
            }

            try
            {
                string fontFileName = Path.GetFileName(fontFilePath);
                string destinationPath = Path.Combine(destinationDirectory, fontFileName);

                // Copy font file to destination (only if it doesn't already exist)
                if (!File.Exists(destinationPath))
                {
                    File.Copy(fontFilePath, destinationPath, false); // Don't overwrite if exists
                }

                // On Windows, add font to system registry
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    AddFontToRegistry(fontFileName);

                    // Load the font resource temporarily
                    AddFontResource(destinationPath);
                }
                // On macOS, we don't need to add to registry - the file placement is sufficient
                // On Linux, fonts are recognized by the fontconfig system after placement
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Re-throw to be handled by caller
            }
            catch (Exception)
            {
                throw; // Re-throw to be handled by caller
            }
        }

        public void AddFontToRegistry(string fontFileName)
        {
            // This method is Windows-specific
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return; // Don't try to access Windows registry on other platforms
            }

            try
            {
                // Registry key for installed fonts
                using (RegistryKey? fontsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", true))
                {
                    if (fontsKey != null)
                    {
                        // Determine if it's a TrueType font
                        bool isTrueType = fontFileName.ToLower().EndsWith(".ttf") || 
                                         fontFileName.ToLower().EndsWith(".ttc") ||
                                         fontFileName.ToLower().EndsWith(".otf");

                        // Create registry value name (display name of the font)
                        string registryValueName = GetFontNameFromFileName(fontFileName);
                        
                        if (isTrueType)
                        {
                            registryValueName += " (TrueType)";
                        }

                        // Set the registry value to the font file name
                        fontsKey.SetValue(registryValueName, fontFileName, RegistryValueKind.String);
                    }
                }
            }
            catch (Exception)
            {
                // In a real scenario, you'd want to log this error
                // For now, we'll let it fail silently as the font may still work
            }
        }

        public string GetFontNameFromFileName(string fileName)
        {
            // Remove extension and clean up the name
            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            // Replace hyphens and underscores with spaces for better readability
            nameWithoutExt = nameWithoutExt.Replace('-', ' ').Replace('_', ' ');
            return nameWithoutExt;
        }

        public void NotifyFontChange()
        {
            // Different platforms have different ways to notify of font changes
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // Send a WM_FONTCHANGE message to all windows to notify of font changes
                    SendMessage(new IntPtr(HWND_BROADCAST), WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
                }
                catch (Exception)
                {
                    // In a real scenario, you'd want to log this error
                }
            }
            // On macOS and Linux, font changes are typically detected automatically
            // or can be refreshed with fc-cache command (Linux)
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    // Refresh font cache on Linux
                    Process process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "fc-cache",
                            Arguments = "-fv",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }
                catch (Exception)
                {
                    // If fc-cache fails, just continue - fonts will still be available after reboot or login
                }
            }
            // On macOS, no specific notification is needed as the system detects font changes
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var fontInstaller = new FontInstallerService();
            
            string osName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : 
                           RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Unix/Linux";
            
            Console.WriteLine($"Font Installer for {osName}");
            Console.WriteLine("================================");

            // Check for admin privileges
            if (!fontInstaller.IsAdministrator())
            {
                Console.WriteLine("ERROR: This application requires administrator privileges.");
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.WriteLine("Please run as Administrator.");
                }
                else
                {
                    Console.WriteLine("Please run with sudo.");
                }
                
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            string? fontSourcePath = null;
            string? fontDestinationPath = null;
            bool useEmbeddedFonts = false;

            // Parse command line arguments
            if (args.Length > 0)
            {
                if (args[0].Equals("--embedded", StringComparison.OrdinalIgnoreCase))
                {
                    // User wants to install embedded fonts
                    useEmbeddedFonts = true;
                    
                    if (args.Length > 1)
                    {
                        fontDestinationPath = args[1];
                    }
                    else
                    {
                        // Default to system Fonts directory
                        fontDestinationPath = fontInstaller.GetSystemFontsDirectory();
                    }
                }
                else
                {
                    // Traditional source/destination approach
                    fontSourcePath = args[0];
                    
                    if (args.Length > 1)
                    {
                        fontDestinationPath = args[1];
                    }
                    else
                    {
                        // Default to system Fonts directory
                        fontDestinationPath = fontInstaller.GetSystemFontsDirectory();
                    }
                }
            }
            else
            {
                // Default behavior: Install embedded fonts to the system Fonts directory
                useEmbeddedFonts = true;
                fontDestinationPath = fontInstaller.GetSystemFontsDirectory();
            }

            if (!useEmbeddedFonts && !Directory.Exists(fontSourcePath))
            {
                Console.WriteLine($"ERROR: Source directory does not exist: {fontSourcePath}");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  fontinstaller [source_directory] [destination_directory]");
                Console.WriteLine("  fontinstaller --embedded [destination_directory]");
                Console.WriteLine("  If no arguments provided, installs embedded fonts to system Fonts directory");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            if (!Directory.Exists(fontDestinationPath))
            {
                Console.WriteLine($"ERROR: Destination directory does not exist: {fontDestinationPath}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            try
            {
                if (useEmbeddedFonts)
                {
                    Console.WriteLine($"Installing embedded fonts to: {fontDestinationPath}");
                    fontInstaller.InstallEmbeddedFonts(fontDestinationPath);
                }
                else
                {
                    Console.WriteLine($"Installing fonts from: {fontSourcePath}");
                    Console.WriteLine($"Installing to: {fontDestinationPath}");
                    fontInstaller.InstallFonts(fontSourcePath!, fontDestinationPath);
                }
                
                Console.WriteLine("\nFont installation completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR: {ex.Message}");
            }
            
            Console.WriteLine("Press any key to exit...");
            // Only wait for key press if launched from console, not from IDE/debugger
            if (!Debugger.IsAttached)
            {
                Console.ReadKey();
            }
        }
    }
}
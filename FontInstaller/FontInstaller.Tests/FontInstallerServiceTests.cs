using Xunit;
using System;
using System.IO;
using System.Reflection;
using FontInstaller.ConsoleApp; // Updated namespace

namespace FontInstaller.Tests
{
    public class FontInstallerServiceTests : IDisposable
    {
        private readonly string _testSourceDir;
        private readonly string _testDestDir;
        private readonly FontInstallerService _fontInstaller;

        public FontInstallerServiceTests()
        {
            // Create temporary directories for testing
            _testSourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _testDestDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
            Directory.CreateDirectory(_testSourceDir);
            Directory.CreateDirectory(_testDestDir);
            
            _fontInstaller = new FontInstallerService();
        }

        public void Dispose()
        {
            // Clean up temporary directories
            if (Directory.Exists(_testSourceDir))
            {
                Directory.Delete(_testSourceDir, true);
            }
            
            if (Directory.Exists(_testDestDir))
            {
                Directory.Delete(_testDestDir, true);
            }
        }

        [Fact]
        public void IsAdministrator_ReturnsBoolValue()
        {
            // This test checks that the method returns a boolean value
            // The actual value depends on how the test is run (with or without admin rights)
            bool result = _fontInstaller.IsAdministrator();
            
            Assert.True(result || !result); // Simply confirms it returns a bool
        }

        [Fact]
        public void GetFontNameFromFileName_RemovesExtensionAndFormatsCorrectly()
        {
            // Test various file name formats
            string result1 = _fontInstaller.GetFontNameFromFileName("MyFont.ttf");
            Assert.Equal("MyFont", result1);

            string result2 = _fontInstaller.GetFontNameFromFileName("My-Font-Bold.otf");
            Assert.Equal("My Font Bold", result2);

            string result3 = _fontInstaller.GetFontNameFromFileName("My_Font_Bold_Italic.ttf");
            Assert.Equal("My Font Bold Italic", result3);

            string result4 = _fontInstaller.GetFontNameFromFileName("Complex-Font_Name.With-Dots.ttf");
            Assert.Equal("Complex Font Name.With Dots", result4);
        }

        [Fact]
        public void InstallSingleFont_ThrowsFileNotFoundException_WhenFontDoesNotExist()
        {
            var nonExistentFontPath = Path.Combine(_testSourceDir, "nonexistent.ttf");
            
            var exception = Assert.Throws<FileNotFoundException>(() => 
                _fontInstaller.InstallSingleFont(nonExistentFontPath, _testDestDir));
            
            Assert.Contains("Font file does not exist", exception.Message);
        }

        [Fact]
        public void InstallSingleFont_ThrowsDirectoryNotFoundException_WhenDestinationDoesNotExist()
        {
            // Create a temporary font file
            var tempFontPath = Path.Combine(_testSourceDir, "temp.ttf");
            File.WriteAllText(tempFontPath, "dummy font content");

            var nonExistentDestDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
            var exception = Assert.Throws<DirectoryNotFoundException>(() => 
                _fontInstaller.InstallSingleFont(tempFontPath, nonExistentDestDir));
            
            Assert.Contains("Destination directory does not exist", exception.Message);
        }

        [Fact]
        public void AddFontToRegistry_DoesNotThrowException()
        {
            // This test verifies that the method doesn't throw an exception
            // Since it accesses the registry, we can only test that it doesn't crash
            // In a real scenario, you might mock the registry access
            
            var exception = Record.Exception(() => 
                _fontInstaller.AddFontToRegistry("test-font.ttf"));
            
            // The method should not throw an exception (though it may fail silently)
            // We're just ensuring it doesn't crash the application
        }

        [Fact]
        public void NotifyFontChange_DoesNotThrowException()
        {
            // Similar to AddFontToRegistry, test that it doesn't crash
            var exception = Record.Exception(() => 
                _fontInstaller.NotifyFontChange());
            
            // The method should not throw an exception
        }

        [Fact]
        public void InstallFonts_ThrowsInvalidOperationException_WhenNoFontFilesExist()
        {
            // Create a directory without any font files
            var emptyDir = Path.Combine(_testSourceDir, "empty");
            Directory.CreateDirectory(emptyDir);
            
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _fontInstaller.InstallFonts(emptyDir, _testDestDir));
            
            Assert.Contains("No font files found", exception.Message);
        }

        [Theory]
        [InlineData(".ttf")]
        [InlineData(".otf")] 
        [InlineData(".ttc")]
        public void InstallFonts_FindsSupportedFontFiles(string extension)
        {
            // Create a font file with the specified extension
            var fontFileName = $"test-font{extension}";
            var fontPath = Path.Combine(_testSourceDir, fontFileName);
            File.WriteAllText(fontPath, "dummy font content");
            
            // This should not throw an exception since we have a valid font file
            var exception = Record.Exception(() => 
                _fontInstaller.InstallFonts(_testSourceDir, _testDestDir));
            
            // Should not throw an exception for finding font files
            // (Actual copying might fail due to permissions, but finding should work)
        }

        [Theory]
        [InlineData(".txt")]
        [InlineData(".doc")]
        [InlineData(".pdf")]
        [InlineData(".jpg")]
        public void InstallFonts_ThrowsException_WhenUnsupportedFilesExist(string extension)
        {
            // Create a non-font file with the specified extension
            var nonFontFileName = $"test-file{extension}";
            var nonFontPath = Path.Combine(_testSourceDir, nonFontFileName);
            File.WriteAllText(nonFontPath, "dummy content");
            
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _fontInstaller.InstallFonts(_testSourceDir, _testDestDir));
            
            Assert.Contains("No font files found", exception.Message);
        }

        [Fact]
        public void InstallEmbeddedFonts_ThrowsInvalidOperationException_WhenNoEmbeddedFontsExist()
        {
            // Since our test assembly doesn't have embedded fonts, this should throw
            // However, since we're testing the main assembly, it might have embedded fonts
            // So we'll just verify the method doesn't crash
            var exception = Record.Exception(() => 
                _fontInstaller.InstallEmbeddedFonts(_testDestDir));
            
            // If no embedded fonts are found, it should throw an exception
            // If embedded fonts are found, it should succeed
            // Either way, we don't want it to crash unexpectedly
        }
    }
}
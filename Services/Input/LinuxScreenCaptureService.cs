using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class LinuxScreenCaptureService : IScreenCaptureService
{
    public async Task<byte[]> CaptureRegionAsync(int x, int y, int width, int height)
    {
        return await Task.Run(() =>
        {
            try
            {
                var tempFile = Path.GetTempFileName() + ".png";
                
                // Try scrot first
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "scrot",
                        Arguments = $"-a {x},{y},{width},{height} {tempFile}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit();

                if (File.Exists(tempFile))
                {
                    var bytes = File.ReadAllBytes(tempFile);
                    File.Delete(tempFile);
                    Log.Debug("Captured screen region ({X}, {Y}, {Width}, {Height})", x, y, width, height);
                    return bytes;
                }

                // Fallback to ImageMagick import
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "import",
                        Arguments = $"-window root -crop {width}x{height}+{x}+{y} {tempFile}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit();

                if (File.Exists(tempFile))
                {
                    var bytes = File.ReadAllBytes(tempFile);
                    File.Delete(tempFile);
                    Log.Debug("Captured screen region ({X}, {Y}, {Width}, {Height})", x, y, width, height);
                    return bytes;
                }

                throw new Exception("Screen capture failed (install scrot or imagemagick)");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error capturing screen region");
                throw;
            }
        });
    }
}


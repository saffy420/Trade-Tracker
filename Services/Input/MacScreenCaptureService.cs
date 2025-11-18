using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class MacScreenCaptureService : IScreenCaptureService
{
    public async Task<byte[]> CaptureRegionAsync(int x, int y, int width, int height)
    {
        return await Task.Run(() =>
        {
            try
            {
                var tempFile = Path.GetTempFileName() + ".png";
                var rect = $"{width}x{height}@{x},{y}";
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "screencapture",
                        Arguments = $"-R{rect} {tempFile}",
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

                throw new Exception("Screen capture failed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error capturing screen region");
                throw;
            }
        });
    }
}


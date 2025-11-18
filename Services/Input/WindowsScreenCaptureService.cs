using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class WindowsScreenCaptureService : IScreenCaptureService
{
    public async Task<byte[]> CaptureRegionAsync(int x, int y, int width, int height)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var bitmap = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(bitmap);
                
                graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height));

                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                
                Log.Debug("Captured screen region ({X}, {Y}, {Width}, {Height})", x, y, width, height);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error capturing screen region");
                throw;
            }
        });
    }
}


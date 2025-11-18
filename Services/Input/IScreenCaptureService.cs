using System.Threading.Tasks;

namespace Tracker.Avalonia.Services.Input;

public interface IScreenCaptureService
{
    Task<byte[]> CaptureRegionAsync(int x, int y, int width, int height);
}


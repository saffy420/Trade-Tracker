using System.Threading.Tasks;

namespace Tracker.Avalonia.Services;

public interface IOcrService
{
    Task<string> RecognizeTextAsync(byte[] imageBytes);
    Task<string> RecognizeTextFromRegionAsync(int x, int y, int width, int height);
    Task<bool> FindTextInRegionAsync(string searchText, int x, int y, int width, int height);
}


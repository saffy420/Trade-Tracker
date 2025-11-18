using System.Threading.Tasks;

namespace Tracker.Avalonia.Services.Input;

public interface IMacroClickService
{
    Task LeftClickAsync(int x, int y);
    Task TripleClickAsync(int x, int y);
    Task MoveCursorAsync(int x, int y);
}


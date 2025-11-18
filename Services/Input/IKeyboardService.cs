using System.Threading.Tasks;

namespace Tracker.Avalonia.Services.Input;

public interface IKeyboardService
{
    Task SendTextAsync(string text);
    Task SendKeystrokeAsync(KeyCode key);
    Task<string> GetClipboardTextAsync();
    Task SetClipboardTextAsync(string text);
    Task ClearClipboardAsync();
}

public enum KeyCode
{
    ControlC,
    ControlV,
    ControlA,
    Enter,
    Escape,
    Tab
}


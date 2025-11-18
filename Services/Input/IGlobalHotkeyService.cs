using System;

namespace Tracker.Avalonia.Services.Input;

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler<HotkeyEventArgs>? HotkeyPressed;
    void RegisterHotkey(int id, uint modifiers, uint vk);
    void UnregisterHotkey(int id);
}

public class HotkeyEventArgs : EventArgs
{
    public int HotkeyId { get; set; }
}


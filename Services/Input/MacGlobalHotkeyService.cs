using System;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class MacGlobalHotkeyService : IGlobalHotkeyService
{
    public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    public MacGlobalHotkeyService()
    {
        Log.Warning("Global hotkeys not implemented for macOS");
    }

    public void RegisterHotkey(int id, uint modifiers, uint vk)
    {
        Log.Warning("Global hotkey registration not implemented for macOS");
    }

    public void UnregisterHotkey(int id)
    {
        // Not implemented
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}


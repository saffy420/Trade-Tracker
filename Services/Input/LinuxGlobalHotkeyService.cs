using System;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class LinuxGlobalHotkeyService : IGlobalHotkeyService
{
    public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    public LinuxGlobalHotkeyService()
    {
        Log.Warning("Global hotkeys not implemented for Linux");
    }

    public void RegisterHotkey(int id, uint modifiers, uint vk)
    {
        Log.Warning("Global hotkey registration not implemented for Linux");
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


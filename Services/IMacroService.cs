using System;
using System.Threading.Tasks;

namespace Tracker.Avalonia.Services;

public interface IMacroService
{
    event EventHandler<string>? StatusUpdated;
    event EventHandler<WindowMoveEventArgs>? WindowMoveRequested;
    event EventHandler? WindowRestoreRequested;
    event EventHandler<string>? FilterGameRequested;
    
    Task RunF1MacroAsync();
    Task RunF2MacroAsync();
    Task RunF3MacroAsync();
    void AbortF2Macro();
}

public class WindowMoveEventArgs : EventArgs
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}


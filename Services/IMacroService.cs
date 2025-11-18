using System;
using System.Threading.Tasks;

namespace Tracker.Avalonia.Services;

public interface IMacroService
{
    event EventHandler<string>? StatusUpdated;
    Task RunF1MacroAsync();
    Task RunF2MacroAsync();
    Task RunF3MacroAsync();
    void AbortF2Macro();
}


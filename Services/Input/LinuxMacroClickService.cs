using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class LinuxMacroClickService : IMacroClickService
{
    public async Task LeftClickAsync(int x, int y)
    {
        await Task.Run(() =>
        {
            try
            {
                RunCommand("xdotool", $"mousemove {x} {y} click 1");
                Log.Debug("Left click at ({X}, {Y})", x, y);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error performing left click (ensure xdotool is installed)");
                throw;
            }
        });
    }

    public async Task TripleClickAsync(int x, int y)
    {
        await Task.Run(() =>
        {
            try
            {
                RunCommand("xdotool", $"mousemove {x} {y} click --repeat 3 --delay 50 1");
                Log.Debug("Triple click at ({X}, {Y})", x, y);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error performing triple click");
                throw;
            }
        });
    }

    public async Task MoveCursorAsync(int x, int y)
    {
        await Task.Run(() =>
        {
            try
            {
                RunCommand("xdotool", $"mousemove {x} {y}");
                Log.Debug("Moved cursor to ({X}, {Y})", x, y);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error moving cursor");
                throw;
            }
        });
    }

    private void RunCommand(string command, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
    }
}


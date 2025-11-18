using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class MacMacroClickService : IMacroClickService
{
    public async Task LeftClickAsync(int x, int y)
    {
        await Task.Run(() =>
        {
            try
            {
                var script = $"tell application \"System Events\" to click at {{{x}, {y}}}";
                RunAppleScript(script);
                Log.Debug("Left click at ({X}, {Y})", x, y);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error performing left click");
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
                for (int i = 0; i < 3; i++)
                {
                    var script = $"tell application \"System Events\" to click at {{{x}, {y}}}";
                    RunAppleScript(script);
                    System.Threading.Thread.Sleep(50);
                }
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
                var script = $"tell application \"System Events\" to set the position of the mouse to {{{x}, {y}}}";
                RunAppleScript(script);
                Log.Debug("Moved cursor to ({X}, {Y})", x, y);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error moving cursor");
                throw;
            }
        });
    }

    private void RunAppleScript(string script)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e \"{script}\"",
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


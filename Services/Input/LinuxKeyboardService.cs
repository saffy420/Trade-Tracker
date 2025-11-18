using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TextCopy;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class LinuxKeyboardService : IKeyboardService
{
    public async Task SendTextAsync(string text)
    {
        await Task.Run(() =>
        {
            try
            {
                // Use xdotool to send text
                RunCommand("xdotool", $"type --delay 10 \"{text}\"");
                Log.Debug("Sent text via Linux keyboard: {Length} characters", text.Length);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending text via Linux keyboard (ensure xdotool is installed)");
                throw;
            }
        });
    }

    public async Task SendKeystrokeAsync(KeyCode key)
    {
        await Task.Run(() =>
        {
            try
            {
                string keyStr = key switch
                {
                    KeyCode.ControlC => "ctrl+c",
                    KeyCode.ControlV => "ctrl+v",
                    KeyCode.ControlA => "ctrl+a",
                    KeyCode.Enter => "Return",
                    KeyCode.Escape => "Escape",
                    KeyCode.Tab => "Tab",
                    _ => throw new NotSupportedException($"Key {key} not supported")
                };
                RunCommand("xdotool", $"key {keyStr}");
                Log.Debug("Sent keystroke via Linux keyboard: {Key}", key);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending keystroke via Linux keyboard");
                throw;
            }
        });
    }

    public async Task<string> GetClipboardTextAsync()
    {
        try
        {
            return await ClipboardService.GetTextAsync() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting clipboard text");
            return string.Empty;
        }
    }

    public async Task SetClipboardTextAsync(string text)
    {
        try
        {
            await ClipboardService.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error setting clipboard text");
        }
    }

    public async Task ClearClipboardAsync()
    {
        try
        {
            await ClipboardService.SetTextAsync(string.Empty);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error clearing clipboard");
        }
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


using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TextCopy;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class MacKeyboardService : IKeyboardService
{
    public async Task SendTextAsync(string text)
    {
        await Task.Run(() =>
        {
            try
            {
                // Use osascript (AppleScript) to send text
                var escapedText = text.Replace("\"", "\\\"");
                var script = $"tell application \"System Events\" to keystroke \"{escapedText}\"";
                RunAppleScript(script);
                Log.Debug("Sent text via Mac keyboard: {Length} characters", text.Length);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending text via Mac keyboard");
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
                string script = key switch
                {
                    KeyCode.ControlC => "tell application \"System Events\" to keystroke \"c\" using command down",
                    KeyCode.ControlV => "tell application \"System Events\" to keystroke \"v\" using command down",
                    KeyCode.ControlA => "tell application \"System Events\" to keystroke \"a\" using command down",
                    KeyCode.Enter => "tell application \"System Events\" to keystroke return",
                    KeyCode.Escape => "tell application \"System Events\" to key code 53",
                    KeyCode.Tab => "tell application \"System Events\" to keystroke tab",
                    _ => throw new NotSupportedException($"Key {key} not supported")
                };
                RunAppleScript(script);
                Log.Debug("Sent keystroke via Mac keyboard: {Key}", key);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending keystroke via Mac keyboard");
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


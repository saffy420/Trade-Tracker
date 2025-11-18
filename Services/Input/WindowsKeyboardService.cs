using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TextCopy;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class WindowsKeyboardService : IKeyboardService
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_RETURN = 0x0D;
    private const byte VK_ESCAPE = 0x1B;
    private const byte VK_TAB = 0x09;

    public async Task SendTextAsync(string text)
    {
        await Task.Run(() =>
        {
            try
            {
                foreach (char c in text)
                {
                    short vk = VkKeyScan(c);
                    byte key = (byte)(vk & 0xFF);
                    
                    keybd_event(key, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    System.Threading.Thread.Sleep(10);
                    keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    System.Threading.Thread.Sleep(10);
                }
                Log.Debug("Sent text via Windows keyboard: {Length} characters", text.Length);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending text via Windows keyboard");
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
                switch (key)
                {
                    case KeyCode.ControlC:
                        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        keybd_event((byte)'C', 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        System.Threading.Thread.Sleep(50);
                        keybd_event((byte)'C', 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        break;
                    case KeyCode.ControlV:
                        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        keybd_event((byte)'V', 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        System.Threading.Thread.Sleep(50);
                        keybd_event((byte)'V', 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        break;
                    case KeyCode.ControlA:
                        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        keybd_event((byte)'A', 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        System.Threading.Thread.Sleep(50);
                        keybd_event((byte)'A', 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        break;
                    case KeyCode.Enter:
                        keybd_event(VK_RETURN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        System.Threading.Thread.Sleep(50);
                        keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        break;
                    case KeyCode.Escape:
                        keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        System.Threading.Thread.Sleep(50);
                        keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        break;
                    case KeyCode.Tab:
                        keybd_event(VK_TAB, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        System.Threading.Thread.Sleep(50);
                        keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        break;
                }
                Log.Debug("Sent keystroke via Windows keyboard: {Key}", key);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending keystroke via Windows keyboard");
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
}


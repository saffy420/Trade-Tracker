using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class WindowsMacroClickService : IMacroClickService
{
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
    private const uint MOUSEEVENTF_LEFTUP = 0x04;

    public async Task LeftClickAsync(int x, int y)
    {
        await Task.Run(() =>
        {
            try
            {
                SetCursorPos(x, y);
                System.Threading.Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
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
                SetCursorPos(x, y);
                System.Threading.Thread.Sleep(50);
                
                // First click
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                
                // Second click
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                
                // Third click
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                
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
                SetCursorPos(x, y);
                Log.Debug("Moved cursor to ({X}, {Y})", x, y);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error moving cursor");
                throw;
            }
        });
    }
}


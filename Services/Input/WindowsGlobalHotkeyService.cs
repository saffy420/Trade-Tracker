using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class WindowsGlobalHotkeyService : IGlobalHotkeyService
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_F1 = 0x70;
    private const int VK_F2 = 0x71;
    private const int VK_F3 = 0x72;
    private const int VK_ESCAPE = 0x1B;
    private const short KEY_PRESSED = unchecked((short)0x8000);

    private readonly Dictionary<int, bool> _keyStates = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;

    public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    public WindowsGlobalHotkeyService()
    {
        _keyStates[VK_F1] = false;
        _keyStates[VK_F2] = false;
        _keyStates[VK_F3] = false;
        _keyStates[VK_ESCAPE] = false;

        // Start polling thread
        Task.Run(() => PollKeysAsync(_cts.Token));
        Log.Information("Global hotkey service initialized with key polling");
    }

    private async Task PollKeysAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                CheckKey(VK_F1, 1);
                CheckKey(VK_F2, 2);
                CheckKey(VK_F3, 3);
                CheckKey(VK_ESCAPE, 4);

                await Task.Delay(50, cancellationToken); // Poll every 50ms
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in hotkey polling");
            }
        }
    }

    private void CheckKey(int vKey, int hotkeyId)
    {
        short state = GetAsyncKeyState(vKey);
        bool isPressed = (state & KEY_PRESSED) != 0;

        if (isPressed && !_keyStates[vKey])
        {
            // Key was just pressed
            _keyStates[vKey] = true;
            OnHotkeyPressed(hotkeyId);
            Log.Debug("Hotkey pressed: ID={Id}, VK={VK}", hotkeyId, vKey);
        }
        else if (!isPressed && _keyStates[vKey])
        {
            // Key was released
            _keyStates[vKey] = false;
        }
    }

    public void RegisterHotkey(int id, uint modifiers, uint vk)
    {
        // Not needed for polling approach - keys are always monitored
        Log.Information("Hotkey monitoring active: ID={Id}", id);
    }

    public void UnregisterHotkey(int id)
    {
        // Not needed for polling approach
        Log.Information("Hotkey unregister requested: ID={Id}", id);
    }

    protected virtual void OnHotkeyPressed(int hotkeyId)
    {
        HotkeyPressed?.Invoke(this, new HotkeyEventArgs { HotkeyId = hotkeyId });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposed = true;
            Log.Information("Global hotkey service disposed");
        }
    }
}


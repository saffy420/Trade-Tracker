using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class MacGlobalHotkeyService : IGlobalHotkeyService
{
    // macOS virtual key codes
    private const int kVK_F1 = 0x7A;
    private const int kVK_F2 = 0x78;
    private const int kVK_F3 = 0x63;
    private const int kVK_Escape = 0x35;

    private readonly Dictionary<int, bool> _keyStates = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;

    public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    public MacGlobalHotkeyService()
    {
        _keyStates[kVK_F1] = false;
        _keyStates[kVK_F2] = false;
        _keyStates[kVK_F3] = false;
        _keyStates[kVK_Escape] = false;

        // Check for accessibility permissions
        if (!AXIsProcessTrusted())
        {
            Log.Warning("Accessibility permissions not granted. Global hotkeys will not work.");
            Log.Warning("Please grant accessibility permissions in System Settings > Privacy & Security > Accessibility");

            // Try to prompt for permissions
            var options = CreateCFDictionary("AXTrustedCheckOptionPrompt", true);
            AXIsProcessTrustedWithOptions(options);
            CFRelease(options);
        }
        else
        {
            Log.Information("Accessibility permissions granted");
        }

        // Start polling thread
        Task.Run(() => PollKeysAsync(_cts.Token));
        Log.Information("Global hotkey service initialized with key polling on macOS");
    }

    private async Task PollKeysAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                CheckKey(kVK_F1, 1);
                CheckKey(kVK_F2, 2);
                CheckKey(kVK_F3, 3);
                CheckKey(kVK_Escape, 4);

                await Task.Delay(50, cancellationToken); // Poll every 50ms
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in hotkey polling on macOS");
            }
        }
    }

    private void CheckKey(int keyCode, int hotkeyId)
    {
        bool isPressed = CGEventSourceKeyState(1, keyCode); // 1 = kCGEventSourceStateHIDSystemState

        if (isPressed && !_keyStates[keyCode])
        {
            // Key was just pressed
            _keyStates[keyCode] = true;
            OnHotkeyPressed(hotkeyId);
            Log.Debug("Hotkey pressed: ID={Id}, KeyCode={KeyCode}", hotkeyId, keyCode);
        }
        else if (!isPressed && _keyStates[keyCode])
        {
            // Key was released
            _keyStates[keyCode] = false;
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

    // P/Invoke declarations
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern bool CGEventSourceKeyState(int stateID, int keyCode);

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern bool AXIsProcessTrusted();

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern bool AXIsProcessTrustedWithOptions(IntPtr options);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFStringCreateWithCString(IntPtr alloc, string str, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFDictionaryCreate(IntPtr allocator, IntPtr[] keys, IntPtr[] values, int numValues, IntPtr keyCallBacks, IntPtr valueCallBacks);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    private static IntPtr CreateCFDictionary(string key, bool value)
    {
        var keyPtr = CFStringCreateWithCString(IntPtr.Zero, key, 0x08000100); // kCFStringEncodingUTF8
        var valuePtr = GetCFBoolean(value);

        var keys = new IntPtr[] { keyPtr };
        var values = new IntPtr[] { valuePtr };

        var dict = CFDictionaryCreate(IntPtr.Zero, keys, values, 1, IntPtr.Zero, IntPtr.Zero);

        CFRelease(keyPtr);

        return dict;
    }

    private static IntPtr GetCFBoolean(bool value)
    {
        var lib = NativeLibrary.Load("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation");
        var symbol = value ? "kCFBooleanTrue" : "kCFBooleanFalse";
        var ptr = NativeLibrary.GetExport(lib, symbol);
        return Marshal.ReadIntPtr(ptr);
    }
}


using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TextCopy;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class MacKeyboardService : IKeyboardService
{
    private const int kVK_ANSI_C = 0x08;
    private const int kVK_ANSI_V = 0x09;
    private const int kVK_ANSI_A = 0x00;
    private const int kVK_Return = 0x24;
    private const int kVK_Tab = 0x30;
    private const int kVK_Escape = 0x35;

    private const ulong kCGEventFlagMaskCommand = 0x100000;

    public async Task SendTextAsync(string text)
    {
        await Task.Run(() =>
        {
            try
            {
                // Send text character by character using CGEvents
                foreach (char c in text)
                {
                    var keyDownEvent = CGEventCreateKeyboardEvent(IntPtr.Zero, 0, true);
                    CGEventKeyboardSetUnicodeString(keyDownEvent, 1, new ushort[] { c });
                    CGEventPost(CGEventTapLocation.HID, keyDownEvent);
                    CFRelease(keyDownEvent);

                    var keyUpEvent = CGEventCreateKeyboardEvent(IntPtr.Zero, 0, false);
                    CGEventKeyboardSetUnicodeString(keyUpEvent, 1, new ushort[] { c });
                    CGEventPost(CGEventTapLocation.HID, keyUpEvent);
                    CFRelease(keyUpEvent);

                    System.Threading.Thread.Sleep(10); // Small delay between characters
                }
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
                switch (key)
                {
                    case KeyCode.ControlC:
                        SendKeyWithModifier(kVK_ANSI_C, kCGEventFlagMaskCommand);
                        break;
                    case KeyCode.ControlV:
                        SendKeyWithModifier(kVK_ANSI_V, kCGEventFlagMaskCommand);
                        break;
                    case KeyCode.ControlA:
                        SendKeyWithModifier(kVK_ANSI_A, kCGEventFlagMaskCommand);
                        break;
                    case KeyCode.Enter:
                        SendKey(kVK_Return);
                        break;
                    case KeyCode.Escape:
                        SendKey(kVK_Escape);
                        break;
                    case KeyCode.Tab:
                        SendKey(kVK_Tab);
                        break;
                    default:
                        throw new NotSupportedException($"Key {key} not supported");
                }
                Log.Debug("Sent keystroke via Mac keyboard: {Key}", key);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending keystroke via Mac keyboard");
                throw;
            }
        });
    }

    private void SendKey(int keyCode)
    {
        var keyDownEvent = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)keyCode, true);
        CGEventPost(CGEventTapLocation.HID, keyDownEvent);
        CFRelease(keyDownEvent);

        var keyUpEvent = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)keyCode, false);
        CGEventPost(CGEventTapLocation.HID, keyUpEvent);
        CFRelease(keyUpEvent);
    }

    private void SendKeyWithModifier(int keyCode, ulong modifierFlags)
    {
        var keyDownEvent = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)keyCode, true);
        CGEventSetFlags(keyDownEvent, modifierFlags);
        CGEventPost(CGEventTapLocation.HID, keyDownEvent);
        CFRelease(keyDownEvent);

        var keyUpEvent = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)keyCode, false);
        CGEventSetFlags(keyUpEvent, modifierFlags);
        CGEventPost(CGEventTapLocation.HID, keyUpEvent);
        CFRelease(keyUpEvent);
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

    // P/Invoke declarations for CoreGraphics
    private enum CGEventTapLocation
    {
        HID = 0,
        Session = 1,
        AnnotatedSession = 2
    }

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort virtualKey, bool keyDown);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventKeyboardSetUnicodeString(IntPtr eventRef, ulong stringLength, ushort[] unicodeString);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventPost(CGEventTapLocation tap, IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventSetFlags(IntPtr eventRef, ulong flags);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);
}


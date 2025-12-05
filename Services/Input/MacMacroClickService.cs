using System;
using System.Runtime.InteropServices;
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
                var point = new CGPoint { x = x, y = y };

                // Move to position
                var moveEvent = CGEventCreateMouseEvent(IntPtr.Zero, CGEventType.MouseMoved, point, CGMouseButton.Left);
                CGEventPost(CGEventTapLocation.HID, moveEvent);
                CFRelease(moveEvent);

                // Mouse down
                var downEvent = CGEventCreateMouseEvent(IntPtr.Zero, CGEventType.LeftMouseDown, point, CGMouseButton.Left);
                CGEventPost(CGEventTapLocation.HID, downEvent);
                CFRelease(downEvent);

                // Mouse up
                var upEvent = CGEventCreateMouseEvent(IntPtr.Zero, CGEventType.LeftMouseUp, point, CGMouseButton.Left);
                CGEventPost(CGEventTapLocation.HID, upEvent);
                CFRelease(upEvent);

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
                var point = new CGPoint { x = x, y = y };

                // Move to position
                var moveEvent = CGEventCreateMouseEvent(IntPtr.Zero, CGEventType.MouseMoved, point, CGMouseButton.Left);
                CGEventPost(CGEventTapLocation.HID, moveEvent);
                CFRelease(moveEvent);

                for (int i = 1; i <= 3; i++)
                {
                    // Mouse down with click count
                    var downEvent = CGEventCreateMouseEvent(IntPtr.Zero, CGEventType.LeftMouseDown, point, CGMouseButton.Left);
                    CGEventSetIntegerValueField(downEvent, CGEventField.MouseEventClickState, i);
                    CGEventPost(CGEventTapLocation.HID, downEvent);
                    CFRelease(downEvent);

                    // Mouse up with click count
                    var upEvent = CGEventCreateMouseEvent(IntPtr.Zero, CGEventType.LeftMouseUp, point, CGMouseButton.Left);
                    CGEventSetIntegerValueField(upEvent, CGEventField.MouseEventClickState, i);
                    CGEventPost(CGEventTapLocation.HID, upEvent);
                    CFRelease(upEvent);

                    if (i < 3) System.Threading.Thread.Sleep(50);
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
                var point = new CGPoint { x = x, y = y };
                var moveEvent = CGEventCreateMouseEvent(IntPtr.Zero, CGEventType.MouseMoved, point, CGMouseButton.Left);
                CGEventPost(CGEventTapLocation.HID, moveEvent);
                CFRelease(moveEvent);

                Log.Debug("Moved cursor to ({X}, {Y})", x, y);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error moving cursor");
                throw;
            }
        });
    }

    // P/Invoke declarations for CoreGraphics
    [StructLayout(LayoutKind.Sequential)]
    private struct CGPoint
    {
        public double x;
        public double y;
    }

    private enum CGEventType
    {
        Null = 0,
        LeftMouseDown = 1,
        LeftMouseUp = 2,
        RightMouseDown = 3,
        RightMouseUp = 4,
        MouseMoved = 5,
        LeftMouseDragged = 6,
        RightMouseDragged = 7
    }

    private enum CGMouseButton
    {
        Left = 0,
        Right = 1,
        Center = 2
    }

    private enum CGEventTapLocation
    {
        HID = 0,
        Session = 1,
        AnnotatedSession = 2
    }

    private enum CGEventField
    {
        MouseEventClickState = 1
    }

    [Flags]
    private enum CGEventFlags : ulong
    {
        MaskCommand = 0x100000
    }

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGEventCreateMouseEvent(IntPtr source, CGEventType mouseType, CGPoint mouseCursorPosition, CGMouseButton mouseButton);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventPost(CGEventTapLocation tap, IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventSetIntegerValueField(IntPtr eventRef, CGEventField field, long value);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventSetFlags(IntPtr eventRef, CGEventFlags flags);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);
}


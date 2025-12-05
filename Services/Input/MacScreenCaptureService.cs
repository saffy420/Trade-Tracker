using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;

namespace Tracker.Avalonia.Services.Input;

public class MacScreenCaptureService : IScreenCaptureService
{
    public async Task<byte[]> CaptureRegionAsync(int x, int y, int width, int height)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Create CGRect for the region
                var rect = new CGRect
                {
                    origin = new CGPoint { x = x, y = y },
                    size = new CGSize { width = width, height = height }
                };

                // Capture the screen region as CGImage
                var imageRef = CGWindowListCreateImage(rect,
                    CGWindowListOption.OptionOnScreenOnly,
                    0, // kCGNullWindowID - capture all windows
                    CGWindowImageOption.Default);

                if (imageRef == IntPtr.Zero)
                {
                    Log.Error("Failed to capture screen region - CGWindowListCreateImage returned null");
                    throw new Exception("Screen capture failed - may need Screen Recording permission");
                }

                try
                {
                    // Convert CGImage to PNG data
                    var pngData = CGImageToPNG(imageRef);
                    Log.Debug("Captured screen region ({X}, {Y}, {Width}, {Height}) - {Size} bytes",
                        x, y, width, height, pngData.Length);
                    return pngData;
                }
                finally
                {
                    CGImageRelease(imageRef);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error capturing screen region");
                throw;
            }
        });
    }

    private byte[] CGImageToPNG(IntPtr imageRef)
    {
        // Create a mutable data object
        var data = CFDataCreateMutable(IntPtr.Zero, 0);

        // Create image destination (PNG format)
        var type = CFStringCreateWithCString(IntPtr.Zero, "public.png", 0x08000100);
        var dest = CGImageDestinationCreateWithData(data, type, 1, IntPtr.Zero);
        CFRelease(type);

        if (dest == IntPtr.Zero)
        {
            CFRelease(data);
            throw new Exception("Failed to create image destination");
        }

        // Add the image to the destination
        CGImageDestinationAddImage(dest, imageRef, IntPtr.Zero);

        // Finalize the image destination
        if (!CGImageDestinationFinalize(dest))
        {
            CFRelease(dest);
            CFRelease(data);
            throw new Exception("Failed to finalize image destination");
        }

        // Get the data as byte array
        var length = CFDataGetLength(data);
        var bytes = new byte[length];
        var range = new CFRange { location = 0, length = length };
        CFDataGetBytes(data, range, bytes);

        CFRelease(dest);
        CFRelease(data);

        return bytes;
    }

    // P/Invoke declarations
    [StructLayout(LayoutKind.Sequential)]
    private struct CGPoint
    {
        public double x;
        public double y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CGSize
    {
        public double width;
        public double height;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect
    {
        public CGPoint origin;
        public CGSize size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CFRange
    {
        public long location;
        public long length;
    }

    [Flags]
    private enum CGWindowListOption : uint
    {
        OptionAll = 0,
        OptionOnScreenOnly = 1,
        OptionOnScreenAboveWindow = 2,
        OptionOnScreenBelowWindow = 4,
        OptionIncludingWindow = 8,
        ExcludeDesktopElements = 16
    }

    [Flags]
    private enum CGWindowImageOption : uint
    {
        Default = 0,
        BoundsIgnoreFraming = 1,
        ShouldBeOpaque = 2,
        OnlyShadows = 4,
        BestResolution = 8,
        NominalResolution = 16
    }

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGWindowListCreateImage(CGRect screenBounds, CGWindowListOption listOption, uint windowID, CGWindowImageOption imageOption);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGImageRelease(IntPtr image);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFDataCreateMutable(IntPtr allocator, long capacity);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFStringCreateWithCString(IntPtr alloc, string str, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern long CFDataGetLength(IntPtr data);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFDataGetBytes(IntPtr data, CFRange range, byte[] buffer);

    [DllImport("/System/Library/Frameworks/ImageIO.framework/ImageIO")]
    private static extern IntPtr CGImageDestinationCreateWithData(IntPtr data, IntPtr type, long count, IntPtr options);

    [DllImport("/System/Library/Frameworks/ImageIO.framework/ImageIO")]
    private static extern void CGImageDestinationAddImage(IntPtr dest, IntPtr image, IntPtr properties);

    [DllImport("/System/Library/Frameworks/ImageIO.framework/ImageIO")]
    private static extern bool CGImageDestinationFinalize(IntPtr dest);
}


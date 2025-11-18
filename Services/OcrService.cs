using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tesseract;
using Tracker.Avalonia.Services.Input;
using Serilog;

namespace Tracker.Avalonia.Services;

public class OcrService : IOcrService
{
    private readonly IScreenCaptureService _screenCapture;
    private readonly string _tessDataPath;

    public OcrService(IScreenCaptureService screenCapture)
    {
        _screenCapture = screenCapture;
        _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

        // Verify tessdata exists
        if (!Directory.Exists(_tessDataPath))
        {
            Log.Error("tessdata folder not found at {Path} - OCR will not work!", _tessDataPath);
        }
        else
        {
            var engFile = Path.Combine(_tessDataPath, "eng.traineddata");
            if (!File.Exists(engFile))
            {
                Log.Error("eng.traineddata not found at {Path} - OCR will not work!", engFile);
            }
            else
            {
                Log.Information("Tesseract initialized successfully with tessdata at {Path}", _tessDataPath);
            }
        }
    }

    public async Task<string> RecognizeTextAsync(byte[] imageBytes)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Preprocess image for better OCR
                var processedBytes = PreprocessImage(imageBytes);
                
                var tempFile = Path.GetTempFileName() + ".png";
                File.WriteAllBytes(tempFile, processedBytes);

                try
                {
                    using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                    using var img = Pix.LoadFromFile(tempFile);
                    using var page = engine.Process(img);
                    
                    string text = page.GetText().Trim();
                    text = CleanOcrText(text);
                    
                    Log.Debug("OCR recognized text: {Text}", text);
                    return text;
                }
                finally
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error performing OCR");
                return string.Empty;
            }
        });
    }

    public async Task<string> RecognizeTextFromRegionAsync(int x, int y, int width, int height)
    {
        try
        {
            var imageBytes = await _screenCapture.CaptureRegionAsync(x, y, width, height);
            return await RecognizeTextAsync(imageBytes);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error recognizing text from region");
            return string.Empty;
        }
    }

    public async Task<bool> FindTextInRegionAsync(string searchText, int x, int y, int width, int height)
    {
        try
        {
            // Capture the region
            var imageBytes = await _screenCapture.CaptureRegionAsync(x, y, width, height);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Log.Warning("Screen capture returned empty image for region ({X},{Y}) {W}x{H}", x, y, width, height);
                return false;
            }
            
            // Preprocess for better OCR
            var processedBytes = PreprocessForButton(imageBytes);
            
            // Run OCR
            var text = await RecognizeTextDirectAsync(processedBytes);
            
            // Check if searchText is contained in the result (case-insensitive, trimmed)
            var cleanText = text.Replace(" ", "").Replace("\n", "").Replace("\r", "").ToLowerInvariant();
            var cleanSearch = searchText.Replace(" ", "").ToLowerInvariant();
            bool found = cleanText.Contains(cleanSearch);
            
            Log.Information("OCR Search - Region: ({X},{Y}) {W}x{H} | Found text: '{Text}' | Searching for: '{SearchText}' | Match: {Found}", 
                x, y, width, height, text, searchText, found);
            return found;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error finding text in region ({X},{Y}) {W}x{H}", x, y, width, height);
            return false;
        }
    }

    private async Task<string> RecognizeTextDirectAsync(byte[] imageBytes)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(_tessDataPath))
                {
                    Log.Error("Cannot perform OCR - tessdata folder missing at {Path}", _tessDataPath);
                    return string.Empty;
                }

                var tempFile = Path.GetTempFileName() + ".png";
                File.WriteAllBytes(tempFile, imageBytes);

                try
                {
                    using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                    using var img = Pix.LoadFromFile(tempFile);
                    using var page = engine.Process(img);
                    
                    string text = page.GetText().Trim();
                    Log.Debug("OCR result: '{Text}'", text);
                    return text;
                }
                finally
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error performing OCR - check if tessdata/eng.traineddata exists");
                return string.Empty;
            }
        });
    }

    private byte[] PreprocessForButton(byte[] imageBytes)
    {
        try
        {
            using var image = Image.Load<Rgba32>(imageBytes);
            
            // Scale up 4x for better OCR on small button text
            var scale = 4;
            var newWidth = image.Width * scale;
            var newHeight = image.Height * scale;
            
            image.Mutate(ctx => ctx
                .Resize(newWidth, newHeight)           // Upscale 4x
                .Grayscale()                           // Convert to grayscale
                .BinaryThreshold(0.65f)                // High contrast
                .Pad(30, 30, Color.White)              // Add white padding for context
            );

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error preprocessing image, using original");
            return imageBytes;
        }
    }

    private byte[] PreprocessImage(byte[] imageBytes)
    {
        try
        {
            using var image = Image.Load<Rgba32>(imageBytes);
            
            // Convert to grayscale and apply thresholding for better OCR
            image.Mutate(ctx => ctx
                .Grayscale()
                .BinaryThreshold(0.5f)
            );

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error preprocessing image, using original");
            return imageBytes;
        }
    }

    private string CleanOcrText(string text)
    {
        // Remove non-alphanumeric characters except spaces, @, ., &, ', -
        text = Regex.Replace(text, @"[^\w\s@.&'\-]", " ");
        // Collapse multiple spaces
        text = Regex.Replace(text, @"\s+", " ");
        // Normalize @ symbols
        text = Regex.Replace(text, @"\s*@\s*", " @ ");
        // Remove trailing "= something" patterns
        text = Regex.Replace(text, @"\s*=\s*\w*$", "");
        return text.Trim();
    }
}


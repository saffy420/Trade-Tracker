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

        if (!Directory.Exists(_tessDataPath))
        {
            Log.Warning("tessdata folder not found at {Path}", _tessDataPath);
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
            var text = await RecognizeTextFromRegionAsync(x, y, width, height);
            bool found = text.Contains(searchText, StringComparison.OrdinalIgnoreCase);
            Log.Debug("Search for '{SearchText}' in region: {Found}", searchText, found);
            return found;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error finding text in region");
            return false;
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


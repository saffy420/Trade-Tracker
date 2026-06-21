using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tracker.Avalonia.Models;
using Tracker.Avalonia.Services.Input;
using Serilog;

namespace Tracker.Avalonia.Services;

public class MacroService : IMacroService
{
    private readonly ICoordinateProvider _coords;
    private readonly IKeyboardService _keyboard;
    private readonly IMacroClickService _mouse;
    private readonly IOcrService _ocr;
    private readonly ITradeStorageService _tradeStorage;
    private volatile bool _abortF2 = false;

    public event EventHandler<string>? StatusUpdated;
    public event EventHandler<WindowMoveEventArgs>? WindowMoveRequested;
    public event EventHandler? WindowRestoreRequested;
    public event EventHandler<string>? FilterGameRequested;

    public MacroService(
        ICoordinateProvider coords,
        IKeyboardService keyboard,
        IMacroClickService mouse,
        IOcrService ocr,
        ITradeStorageService tradeStorage)
    {
        _coords = coords;
        _keyboard = keyboard;
        _mouse = mouse;
        _ocr = ocr;
        _tradeStorage = tradeStorage;
    }

    private void UpdateStatus(string message)
    {
        Log.Information("Macro Status: {Message}", message);
        StatusUpdated?.Invoke(this, message);
    }

    public async Task RunF1MacroAsync()
    {
        try
        {
            UpdateStatus("F1 Macro Started.");

            // 1. Click "Place Trade" using fallback coordinates
            var fallbackX = (int)_coords.GetDouble("F1Macro", "PlaceTradeFallbackX", 1525);
            var fallbackY = (int)_coords.GetDouble("F1Macro", "PlaceTradeFallbackY", 466);
            await _mouse.LeftClickAsync(fallbackX, fallbackY);
            await _mouse.LeftClickAsync(fallbackX, fallbackY);
            UpdateStatus($"Clicked 'Place Trade' at ({fallbackX}, {fallbackY}).");

            // 2. Triple-click and copy Game Name
            var gameX = _coords.GetInt("F1Macro", "GameNameX");
            var gameY = _coords.GetInt("F1Macro", "GameNameY");
            await _mouse.LeftClickAsync(gameX, gameY);
            await Task.Delay(50);
            await _mouse.TripleClickAsync(gameX, gameY);
            UpdateStatus("Triple-clicked Game Name.");
            await Task.Delay(150);

            await _keyboard.SendKeystrokeAsync(KeyCode.ControlC);
            await Task.Delay(100);

            string game = await _keyboard.GetClipboardTextAsync();
            game = game.Trim();
            UpdateStatus($"Copied Game: [{game}]");

            // 3. Triple-click and copy Trade Type
            var tradeX = _coords.GetInt("F1Macro", "TradeTypeX");
            var tradeY = _coords.GetInt("F1Macro", "TradeTypeY");
            await _mouse.TripleClickAsync(tradeX, tradeY);
            UpdateStatus("Triple-clicked Trade Type.");
            await Task.Delay(150);

            await _keyboard.SendKeystrokeAsync(KeyCode.ControlC);
            await Task.Delay(100);

            string market = await _keyboard.GetClipboardTextAsync();
            market = market.Trim();
            UpdateStatus($"Copied Trade Type: [{market}]");

            // 4. Validate and save
            if (string.IsNullOrEmpty(game) || string.IsNullOrEmpty(market))
            {
                UpdateStatus("F1: Copied game or market was empty.");
                return;
            }

            await _tradeStorage.SaveTradeAsync(new Trade { Game = game, Market = market });
            UpdateStatus($"✅ F1: Saved -> {game} | {market}");

            // 5. Additional clicks
            var click1X = _coords.GetInt("F1Macro", "ClickPosition1X");
            var click1Y = _coords.GetInt("F1Macro", "ClickPosition1Y");
            await _mouse.LeftClickAsync(click1X, click1Y);
            UpdateStatus($"Clicked ({click1X}, {click1Y}).");

            // 6. Move cursor to final position
            var finalX = _coords.GetInt("F1Macro", "FinalCursorX");
            var finalY = _coords.GetInt("F1Macro", "FinalCursorY");
            await _mouse.MoveCursorAsync(finalX, finalY);
            UpdateStatus($"Cursor moved to ({finalX}, {finalY}).");

            await _keyboard.ClearClipboardAsync();
            UpdateStatus("F1 Macro Completed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "F1 Macro error");
            UpdateStatus($"F1 Macro Error: {ex.Message}");
        }
    }

    public async Task RunF2MacroAsync()
    {
        _abortF2 = false;
        UpdateStatus("F2 Macro Started.");
        bool windowMoved = false;

        try
        {
            // 1. Click Button
            var btnX = _coords.GetInt("F2Macro", "ButtonX");
            var btnY = _coords.GetInt("F2Macro", "ButtonY");
            await _mouse.LeftClickAsync(btnX, btnY-50);
            await Task.Delay(50);
            await _mouse.LeftClickAsync(btnX, btnY);
            UpdateStatus("Clicked Button.");

            // 2. Copy game name
            var clickAwayX = _coords.GetInt("General", "ClickAwayX");
            var clickAwayY = _coords.GetInt("General", "ClickAwayY");
            var gameX = _coords.GetInt("F2Macro", "GameNameX");
            var gameY = _coords.GetInt("F2Macro", "GameNameY");

            string game = await TryCopyTextWithRetriesAsync(clickAwayX, clickAwayY, gameX, gameY, 2);
            if (string.IsNullOrEmpty(game))
            {
                UpdateStatus("Error getting game from clipboard after retries.");
                return;
            }

            // Check abort after copying game name
            if (_abortF2)
            {
                UpdateStatus("F2 Macro Aborted.");
                return;
            }

            UpdateStatus($"Copied Game: [{game}]");

            // 2.5. Move and resize UI temporarily
            var uiMoveX = _coords.GetInt("F2Macro", "UiMoveX");
            var uiMoveY = _coords.GetInt("F2Macro", "UiMoveY");
            var uiResizeW = _coords.GetInt("F2Macro", "UiResizeWidth");
            var uiResizeH = _coords.GetInt("F2Macro", "UiResizeHeight");
            
            WindowMoveRequested?.Invoke(this, new WindowMoveEventArgs
            {
                X = uiMoveX,
                Y = uiMoveY,
                Width = uiResizeW,
                Height = uiResizeH
            });
            windowMoved = true;
            UpdateStatus($"Moved UI to ({uiMoveX}, {uiMoveY}).");
            await Task.Delay(100); // Give window time to move

            // 3. Validate game for filtering
            if (string.IsNullOrEmpty(game) || !game.Contains("@"))
            {
                UpdateStatus("F2: Copied text failed validation for game filter.");
                return;
            }

            // 3.5. Filter the trade tracker to show only this game's trades
            FilterGameRequested?.Invoke(this, game);
            UpdateStatus($"🔍 F2: Filtering by game -> [{game}]");

            // Check abort before waiting for page load
            if (_abortF2)
            {
                UpdateStatus("F2 Macro Aborted.");
                return;
            }

            // 5. Copy number
            var numX = _coords.GetInt("F2Macro", "NumberX");
            var numY = _coords.GetInt("F2Macro", "NumberY");
            string numberText = await TryCopyTextWithRetriesAsync(clickAwayX, clickAwayY, numX, numY, 2);
            if (string.IsNullOrEmpty(numberText))
            {
                UpdateStatus("Error getting number from clipboard after retries.");
                return;
            }

            // Check abort after copying number
            if (_abortF2)
            {
                UpdateStatus("F2 Macro Aborted.");
                return;
            }

            UpdateStatus($"Copied Number: [{numberText}]");

            // 6. Process number
            double finalNumber = 0;
            bool numberParsed = false;

            if (double.TryParse(Regex.Replace(numberText, @"[\$, ]", ""), out double number))
            {
                UpdateStatus($"Parsed Number: {number}.");
                var maxNumber = _coords.GetDouble("F2Macro", "MaxNumber");
                if (number > maxNumber)
                {
                    number = maxNumber;
                    UpdateStatus($"Capped to {maxNumber}.");
                }
                finalNumber = number;
                numberParsed = true;
                UpdateStatus($"Final Number: {number}.");
            }
            else
            {
                UpdateStatus("Failed to parse number.");
            }

            // Check abort before starting the polling loop
            if (_abortF2)
            {
                UpdateStatus("F2 Macro Aborted.");
                return;
            }

            // 4. Wait for page load - run python macro every 250ms with OCR parameters
            UpdateStatus("Starting python macro polling...");
            WindowRestoreRequested?.Invoke(this, EventArgs.Empty);
            UpdateStatus("Restored UI position.");
            var sw = Stopwatch.StartNew();
            var textFieldX = _coords.GetInt("F2Macro", "TextFieldX"); // Assuming these coordinates exist
            var textFieldY = _coords.GetInt("F2Macro", "TextFieldY");
            var clearX = _coords.GetInt("F2Macro", "ClearAllX");
            var clearY = _coords.GetInt("F2Macro", "ClearAllY");
            var clearW = _coords.GetInt("F2Macro", "ClearAllWidth");
            var clearH = _coords.GetInt("F2Macro", "ClearAllHeight");

            while (sw.ElapsedMilliseconds < 5000) // Reduced timeout to 5 seconds
            {
                if (_abortF2)
                {
                    UpdateStatus("F2 Macro Aborted.");
                    return;
                }

                try
                {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var scriptPath = System.IO.Path.Combine(home, "arvvy", "pyinput_macro.py");

                    var psi = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/env",
                        Arguments = $"python3 \"{scriptPath}\" --x {textFieldX} --y {textFieldY} --ocr-x {clearX} --ocr-y {clearY} --ocr-w {clearW} --ocr-h {clearH}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    UpdateStatus($"Running python macro with parameters: --x {textFieldX} --y {textFieldY} --ocr-x {clearX} --ocr-y {clearY} --ocr-w {clearW} --ocr-h {clearH}");
                    using var proc = Process.Start(psi);
                    if (proc == null)
                    {
                        UpdateStatus("Failed to start python macro process.");
                        await Task.Delay(250);
                        continue; // Continue polling loop
                    }

                    // Wait up to 10 seconds for the script to finish
                    var finished = await Task.Run(() => proc.WaitForExit(10000));
                    if (!finished)
                    {
                        try { proc.Kill(); } catch { }
                        UpdateStatus("Python macro timed out and was killed.");
                        await Task.Delay(250);
                        continue; // Continue polling loop
                    }

                    var outText = await proc.StandardOutput.ReadToEndAsync();
                    var errText = await proc.StandardError.ReadToEndAsync();
                    
                    if (!string.IsNullOrWhiteSpace(errText))
                    {
                        UpdateStatus($"py error: {errText.Trim()}");
                        await Task.Delay(250);
                        continue; // Continue polling loop
                    }

                    if (!string.IsNullOrWhiteSpace(outText))
                    {
                        var output = outText.Trim();
                        if (output == "True")
                        {
                            UpdateStatus("Python macro returned true - F2 Macro completed.");
                            break; // Exit the polling loop
                        }
                        else if (output == "False")
                        {
                            UpdateStatus("Python macro returned false - continuing polling...");
                        }
                        else
                        {
                            UpdateStatus($"Python macro returned: {output} - continuing polling...");
                        }
                    }
                    else
                    {
                        UpdateStatus("Python macro returned no output - continuing polling...");
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error running python macro: {ex.Message} - continuing polling...");
                }

                await Task.Delay(250); // Wait 250ms before next iteration
            }

            if (sw.ElapsedMilliseconds >= 5000)
            {
                UpdateStatus("Timeout waiting for python macro to return true (5s).");
            }

            await _keyboard.ClearClipboardAsync();
            UpdateStatus("F2 Macro Completed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "F2 Macro error");
            UpdateStatus($"F2 Macro Error: {ex.Message}");
        }
        finally
        {
            // Restore window if it was moved
            if (windowMoved)
            {
                WindowRestoreRequested?.Invoke(this, EventArgs.Empty);
                UpdateStatus("Restored UI position.");
            }
        }
    }

    public async Task RunF3MacroAsync()
    {
        try
        {
            UpdateStatus("F3 Macro Started.");

            // 1. Abort F2 macro
            _abortF2 = true;
            UpdateStatus("F2 Macro Aborted via F3.");

            // 2. Click
            var clickX = _coords.GetInt("F3Macro", "ClickX");
            var clickY = _coords.GetInt("F3Macro", "ClickY");
            await _mouse.LeftClickAsync(clickX, clickY);
            UpdateStatus($"Clicked ({clickX}, {clickY}).");

            // 3. Move cursor
            var finalX = _coords.GetInt("F3Macro", "FinalCursorX");
            var finalY = _coords.GetInt("F3Macro", "FinalCursorY");
            await _mouse.LeftClickAsync(finalX, finalY);
            await _mouse.MoveCursorAsync(finalX, finalY);
            UpdateStatus($"Cursor moved to ({finalX}, {finalY}).");

            await _keyboard.ClearClipboardAsync();
            UpdateStatus("F3 Macro Completed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "F3 Macro error");
            UpdateStatus($"F3 Macro Error: {ex.Message}");
        }
    }

    public void AbortF2Macro()
    {
        _abortF2 = true;
    }

    private async Task<string> TryCopyTextWithRetriesAsync(int clickAwayX, int clickAwayY, int targetX, int targetY, int maxAttempts)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Check abort during retries
            if (_abortF2)
            {
                return string.Empty;
            }

            try
            {
                // Click elsewhere to remove focus
                await _mouse.LeftClickAsync(clickAwayX, clickAwayY);
                await Task.Delay(50);

                // Triple-click target
                await _mouse.TripleClickAsync(targetX, targetY);
                await Task.Delay(150);

                // Copy
                await _keyboard.SendKeystrokeAsync(KeyCode.ControlC);
                await Task.Delay(150);

                string text = await _keyboard.GetClipboardTextAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }
            catch
            {
                // Retry
            }

            await Task.Delay(100);
        }

        return string.Empty;
    }
}
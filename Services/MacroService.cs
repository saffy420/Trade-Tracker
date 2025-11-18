using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
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
        ITradeStorageService betStorage)
    {
        _coords = coords;
        _keyboard = keyboard;
        _mouse = mouse;
        _ocr = ocr;
        _tradeStorage = betStorage;
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

            // 1. Find and click "Place Trade"
            var pbX = _coords.GetInt("F1Macro", "PlaceTradeOcrX");
            var pbY = _coords.GetInt("F1Macro", "PlaceTradeOcrY");
            var pbW = _coords.GetInt("F1Macro", "PlaceTradeOcrWidth");
            var pbH = _coords.GetInt("F1Macro", "PlaceTradeOcrHeight");

            bool foundPlaceTrade = await _ocr.FindTextInRegionAsync("Place Trade", pbX, pbY, pbW, pbH);
            if (foundPlaceTrade)
            {
                await _mouse.LeftClickAsync(pbX + pbW / 2, pbY + pbH / 2);
                UpdateStatus("Clicked 'Place Trade'.");
            }
            else
            {
                // Fallback click
                var fallbackX = _coords.GetInt("F1Macro", "PlaceTradeFallbackX");
                var fallbackY = _coords.GetInt("F1Macro", "PlaceTradeFallbackY");
                await _mouse.LeftClickAsync(fallbackX, fallbackY);
                UpdateStatus($"Fallback: Clicked ({fallbackX}, {fallbackY}).");
            }

            // 2. Triple-click and copy Game Name
            var gameX = _coords.GetInt("F1Macro", "GameNameX");
            var gameY = _coords.GetInt("F1Macro", "GameNameY");
            await _mouse.TripleClickAsync(gameX, gameY);
            UpdateStatus("Triple-clicked Game Name.");
            await Task.Delay(150);

            await _keyboard.SendKeystrokeAsync(KeyCode.ControlC);
            await Task.Delay(100);

            string game = await _keyboard.GetClipboardTextAsync();
            game = game.Trim();
            UpdateStatus($"Copied Game: [{game}]");

            // 3. Triple-click and copy Trade Type
            var betX = _coords.GetInt("F1Macro", "TradeTypeX");
            var betY = _coords.GetInt("F1Macro", "TradeTypeY");
            await _mouse.TripleClickAsync(betX, betY);
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

            // 4. Wait for page load - check for "Clear" and OCR box text
            UpdateStatus("Waiting for 'Clear' and OCR box text...");
            var sw = Stopwatch.StartNew();
            bool foundClear = false;
            bool foundOcrText = false;

            var clearX = _coords.GetInt("F2Macro", "ClearAllX");
            var clearY = _coords.GetInt("F2Macro", "ClearAllY");
            var clearW = _coords.GetInt("F2Macro", "ClearAllWidth");
            var clearH = _coords.GetInt("F2Macro", "ClearAllHeight");

            var ocrBoxX = _coords.GetInt("F2Macro", "OcrBoxX");
            var ocrBoxY = _coords.GetInt("F2Macro", "OcrBoxY");
            var ocrBoxW = _coords.GetInt("F2Macro", "OcrBoxWidth");
            var ocrBoxH = _coords.GetInt("F2Macro", "OcrBoxHeight");

            while (sw.ElapsedMilliseconds < 5000)
            {
                if (_abortF2)
                {
                    UpdateStatus("F2 Macro Aborted.");
                    return;
                }

                if (!foundClear)
                {
                    foundClear = await _ocr.FindTextInRegionAsync("Clear", clearX, clearY, clearW, clearH);
                    if (foundClear) UpdateStatus("'Clear' detected.");
                }

                if (!foundOcrText)
                {
                    var ocrText = await _ocr.RecognizeTextFromRegionAsync(ocrBoxX, ocrBoxY, ocrBoxW, ocrBoxH);
                    foundOcrText = !string.IsNullOrWhiteSpace(ocrText);
                    if (foundOcrText) UpdateStatus("OCR box text present.");
                }

                if (foundClear && foundOcrText)
                {
                    UpdateStatus("Both 'Clear' and OCR text detected. Page is loaded.");
                    break;
                }

                await Task.Delay(250);
            }

            if (sw.ElapsedMilliseconds >= 5000)
            {
                UpdateStatus("Timeout waiting for page elements (5s). Proceeding...");
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

            // 7. Click text field
            var textFieldX = _coords.GetInt("F2Macro", "TextFieldX");
            var textFieldY = _coords.GetInt("F2Macro", "TextFieldY");
            await _mouse.LeftClickAsync(textFieldX, textFieldY);
            await Task.Delay(50);
            await _mouse.LeftClickAsync(textFieldX, textFieldY);
            await Task.Delay(25);
            await _mouse.LeftClickAsync(textFieldX, textFieldY);
            UpdateStatus($"Clicked text field at ({textFieldX}, {textFieldY}).");

            // 8. Paste number
            if (numberParsed)
            {
                await Task.Delay(150);
                string numberString = finalNumber.ToString("G15");
                await _keyboard.SendTextAsync(numberString);
                UpdateStatus($"Pasted number: {numberString}.");
            }
            else
            {
                UpdateStatus("Number was not valid, paste skipped.");
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


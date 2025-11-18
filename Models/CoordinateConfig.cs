using System.Collections.Generic;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Tracker.Avalonia.Models;

public class CoordinateConfig
{
    [JsonProperty("F1Macro")]
    public Dictionary<string, object> F1Macro { get; set; } = new();

    [JsonProperty("F2Macro")]
    public Dictionary<string, object> F2Macro { get; set; } = new();

    [JsonProperty("F3Macro")]
    public Dictionary<string, object> F3Macro { get; set; } = new();

    [JsonProperty("General")]
    public Dictionary<string, object> General { get; set; } = new();

    public static CoordinateConfig Default => new()
    {
        F1Macro = new Dictionary<string, object>
        {
            ["PlaceTradeOcrX"] = 1295,
            ["PlaceTradeOcrY"] = 271,
            ["PlaceTradeOcrWidth"] = 375,
            ["PlaceTradeOcrHeight"] = 306,
            ["PlaceTradeFallbackX"] = 1540,
            ["PlaceTradeFallbackY"] = 484,
            ["GameNameX"] = -910,
            ["GameNameY"] = 350,
            ["TradeTypeX"] = -910,
            ["TradeTypeY"] = 375,
            ["ClickPosition1X"] = -824,
            ["ClickPosition1Y"] = 709,
            ["FinalCursorX"] = -1669,
            ["FinalCursorY"] = 224
        },
        F2Macro = new Dictionary<string, object>
        {
            ["ButtonX"] = -1225,
            ["ButtonY"] = 350,
            ["GameNameX"] = -910,
            ["GameNameY"] = 350,
            ["NumberX"] = -610,
            ["NumberY"] = 451,
            ["TextFieldX"] = 1804,
            ["TextFieldY"] = 391,
            ["SearchX"] = 1383,
            ["SearchY"] = 236,
            ["SearchWidth"] = 415,
            ["SearchHeight"] = 791,
            ["ClearAllX"] = 1698,
            ["ClearAllY"] = 256,
            ["ClearAllWidth"] = 55,
            ["ClearAllHeight"] = 20,
            ["OcrBoxX"] = 1337,
            ["OcrBoxY"] = 337,
            ["OcrBoxWidth"] = 389,
            ["OcrBoxHeight"] = 88,
            ["MaxNumber"] = 40.0,
            ["UiMoveX"] = -1090,
            ["UiMoveY"] = 385,
            ["UiResizeWidth"] = 270,
            ["UiResizeHeight"] = 200
        },
        F3Macro = new Dictionary<string, object>
        {
            ["ClickX"] = 1404,
            ["ClickY"] = 378,
            ["FinalCursorX"] = -1669,
            ["FinalCursorY"] = 244
        },
        General = new Dictionary<string, object>
        {
            ["ClickAwayX"] = -206,
            ["ClickAwayY"] = 23
        }
    };
}

public partial class CoordinateEntry : ObservableObject
{
    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private double _value;

    public string FullKey => $"{Category}.{Key}";
}


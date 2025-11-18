using System.Collections.Generic;
using Newtonsoft.Json;

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
            ["PlaceTradeOcrX"] = 1268,
            ["PlaceTradeOcrY"] = 443,
            ["PlaceTradeOcrWidth"] = 559,
            ["PlaceTradeOcrHeight"] = 130,
            ["GameNameX"] = -914,
            ["GameNameY"] = 167,
            ["TradeTypeX"] = -914,
            ["TradeTypeY"] = 192,
            ["ClickPosition1X"] = -824,
            ["ClickPosition1Y"] = 530,
            ["FinalCursorX"] = -206,
            ["FinalCursorY"] = 23
        },
        F2Macro = new Dictionary<string, object>
        {
            ["ButtonX"] = -1225,
            ["ButtonY"] = 170,
            ["GameNameX"] = -914,
            ["GameNameY"] = 167,
            ["NumberX"] = -610,
            ["NumberY"] = 242,
            ["TextFieldX"] = 1685,
            ["TextFieldY"] = 350,
            ["SearchX"] = 1328,
            ["SearchY"] = 202,
            ["SearchWidth"] = 415,
            ["SearchHeight"] = 791,
            ["ClearAllRelX"] = 280,
            ["ClearAllRelY"] = 37,
            ["ClearAllWidth"] = 55,
            ["ClearAllHeight"] = 16,
            ["OcrBoxX"] = 1337,
            ["OcrBoxY"] = 337,
            ["OcrBoxWidth"] = 389,
            ["OcrBoxHeight"] = 88,
            ["MaxNumber"] = 40.0,
            ["UiMoveX"] = -1090,
            ["UiMoveY"] = 250,
            ["UiResizeWidth"] = 270,
            ["UiResizeHeight"] = 200
        },
        F3Macro = new Dictionary<string, object>
        {
            ["ClickX"] = 1351,
            ["ClickY"] = 342,
            ["FinalCursorX"] = -206,
            ["FinalCursorY"] = 23
        },
        General = new Dictionary<string, object>
        {
            ["ClickAwayX"] = -206,
            ["ClickAwayY"] = 23
        }
    };
}

public class CoordinateEntry
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public double Value { get; set; }
    public string FullKey => $"{Category}.{Key}";
}


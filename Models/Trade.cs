using System;

namespace Tracker.Avalonia.Models;

public class Trade
{
    public string Game { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public DateTime AddedDate { get; set; } = DateTime.Now;

    public string NormalizedGame => NormalizeGameName(Game);

    private static string NormalizeGameName(string game)
    {
        if (string.IsNullOrWhiteSpace(game)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(game.Trim(), @"\s*@\s*", " @ ");
    }

    public override bool Equals(object? obj)
    {
        if (obj is Tradeother)
        {
            return string.Equals(NormalizedGame, other.NormalizedGame, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Market.Trim(), other.Market.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            NormalizedGame.ToLowerInvariant(),
            Market.Trim().ToLowerInvariant()
        );
    }
}


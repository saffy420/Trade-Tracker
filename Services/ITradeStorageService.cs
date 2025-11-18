using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tracker.Avalonia.Models;

namespace Tracker.Avalonia.Services;

public interface ITradeStorageService
{
    event EventHandler? TradesChanged;
    Task<List<Trade>> LoadAllTradesAsync();
    Task SaveTradeAsync(Trade trade);
    Task RemoveTradeAsync(Trade trade);
    Task UpdateTradeAsync(Trade oldTrade, Trade newTrade);
    Task<Dictionary<string, List<Trade>>> GetTradesByGameAsync();
    Task ClearAllTradesAsync();
    string TradesFilePath { get; }
}


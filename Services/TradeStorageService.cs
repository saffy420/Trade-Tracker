using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tracker.Avalonia.Models;
using Serilog;

namespace Tracker.Avalonia.Services;

public class TradeStorageService : ITradeStorageService, IDisposable
{
    private readonly string _tradesFilePath;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public event EventHandler? TradesChanged;
    
    public string TradesFilePath => _tradesFilePath;

    public TradeStorageService()
    {
        // Use AppData for cross-platform config storage with migration support
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TradeTracker"
        );

        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        _tradesFilePath = Path.Combine(configDir, "trades.txt");

        // Migrate from old location if needed
        MigrateFromOldLocation();

        // Ensure file exists
        if (!File.Exists(_tradesFilePath))
        {
            File.WriteAllText(_tradesFilePath, string.Empty);
        }

        // Setup file watcher
        _fileWatcher = new FileSystemWatcher(configDir)
        {
            Filter = "trades.txt",
            NotifyFilter = NotifyFilters.LastWrite
        };
        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.EnableRaisingEvents = true;

        Log.Information("TradeStorageService initialized with file: {FilePath}", _tradesFilePath);
    }

    private void MigrateFromOldLocation()
    {
        try
        {
            var oldPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "trades.txt"
            );

            if (File.Exists(oldPath) && !File.Exists(_tradesFilePath))
            {
                File.Copy(oldPath, _tradesFilePath, overwrite: false);
                Log.Information("Migrated trades from {OldPath} to {NewPath}", oldPath, _tradesFilePath);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to migrate trades from old location");
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Thread.Sleep(100); // Debounce
        TradesChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<List<Trade>> LoadAllTradesAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(_tradesFilePath))
            {
                return new List<Trade>();
            }

            var lines = await File.ReadAllLinesAsync(_tradesFilePath);
            var trades = new List<Trade>();

            for (int i = 0; i < lines.Length; i += 2)
            {
                if (i + 1 >= lines.Length) break;

                string game = lines[i].Trim();
                string market = lines[i + 1].Trim();

                if (string.IsNullOrEmpty(game) || !game.Contains("@") || string.IsNullOrEmpty(market))
                    continue;

                trades.Add(new Trade
                {
                    Game = game,
                    Market = market
                });
            }

            Log.Debug("Loaded {Count} trades from file", trades.Count);
            return trades;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading trades from file");
            return new List<Trade>();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveTradeAsync(Trade trade)
    {
        await _fileLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_tradesFilePath, 
                $"{trade.Game}{Environment.NewLine}{trade.Market}{Environment.NewLine}");
            Log.Information("Saved trade: {Game} | {Market}", trade.Game, trade.Market);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving trade");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task RemoveTradeAsync(Trade trade)
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(_tradesFilePath)) return;

            var lines = (await File.ReadAllLinesAsync(_tradesFilePath)).ToList();
            var keep = new List<string>();

            for (int i = 0; i < lines.Count; i += 2)
            {
                if (i + 1 >= lines.Count)
                {
                    keep.Add(lines[i]);
                    break;
                }

                string fileGame = lines[i].Trim();
                string fileMarket = lines[i + 1].Trim();

                var fileTrade = new Trade { Game = fileGame, Market = fileMarket };

                if (!fileTrade.Equals(trade))
                {
                    keep.Add(lines[i]);
                    keep.Add(lines[i + 1]);
                }
            }

            await File.WriteAllLinesAsync(_tradesFilePath, keep);
            Log.Information("Removed trade: {Game} | {Market}", trade.Game, trade.Market);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error removing trade");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task UpdateTradeAsync(Trade oldTrade, Trade newTrade)
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(_tradesFilePath)) return;

            var lines = (await File.ReadAllLinesAsync(_tradesFilePath)).ToList();
            var updated = new List<string>();
            bool foundAndUpdated = false;

            for (int i = 0; i < lines.Count; i += 2)
            {
                if (i + 1 >= lines.Count)
                {
                    updated.Add(lines[i]);
                    break;
                }

                string fileGame = lines[i].Trim();
                string fileMarket = lines[i + 1].Trim();

                var fileTrade = new Trade { Game = fileGame, Market = fileMarket };

                if (!foundAndUpdated && fileTrade.Equals(oldTrade))
                {
                    // Replace with new trade
                    updated.Add(newTrade.Game);
                    updated.Add(newTrade.Market);
                    foundAndUpdated = true;
                }
                else
                {
                    // Keep existing
                    updated.Add(lines[i]);
                    updated.Add(lines[i + 1]);
                }
            }

            await File.WriteAllLinesAsync(_tradesFilePath, updated);
            Log.Information("Updated trade from '{OldGame}|{OldMarket}' to '{NewGame}|{NewMarket}'", 
                oldTrade.Game, oldTrade.Market, newTrade.Game, newTrade.Market);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating trade");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<Dictionary<string, List<Trade>>> GetTradesByGameAsync()
    {
        var trades = await LoadAllTradesAsync();
        var grouped = new Dictionary<string, List<Trade>>(StringComparer.OrdinalIgnoreCase);

        foreach (var trade in trades)
        {
            if (!grouped.ContainsKey(trade.NormalizedGame))
            {
                grouped[trade.NormalizedGame] = new List<Trade>();
            }
            grouped[trade.NormalizedGame].Add(trade);
        }

        return grouped;
    }

    public async Task ClearAllTradesAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            // Write empty file
            await File.WriteAllTextAsync(_tradesFilePath, string.Empty);
            Log.Information("Cleared all trades from file");
            TradesChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error clearing all trades");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
        _fileLock?.Dispose();
    }
}


using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tracker.Avalonia.Models;
using Tracker.Avalonia.Services;
using Tracker.Avalonia.Views;
using Serilog;

namespace Tracker.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ITradeStorageService _tradeStorage;
    private readonly IMacroService _macroService;
    private readonly CoordinateConfigService _coordService;

    [ObservableProperty]
    private ObservableCollection<TradeGroup> _tradeGroups = new();

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string? _currentFilterGame = null;

    [ObservableProperty]
    private int _totalTradeCount = 0;

    [ObservableProperty]
    private int _gameCount = 0;

    public MainWindowViewModel(
        ITradeStorageService betStorage,
        IMacroService macroService,
        CoordinateConfigService coordService)
    {
        _tradeStorage = betStorage;
        _macroService = macroService;
        _coordService = coordService;

        _tradeStorage.TradesChanged += OnTradesChanged;
        _macroService.StatusUpdated += OnMacroStatusUpdated;

        // Load initial trades
        Task.Run(async () => await LoadTradesAsync());
    }

    private void OnTradesChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(async () => await LoadTradesAsync());
    }

    private void OnMacroStatusUpdated(object? sender, string message)
    {
        Dispatcher.UIThread.InvokeAsync(() => StatusMessage = message);
    }

    public async Task LoadTradesAsync()
    {
        try
        {
            var betsDict = await _tradeStorage.GetTradesByGameAsync();

            if (CurrentFilterGame == null)
            {
                // Show all trades grouped by game
                var groups = betsDict
                    .OrderByDescending(kvp => kvp.Value.Count)
                    .Select(kvp => new TradeGroup
                    {
                        GameName = kvp.Key,
                        Trades = new ObservableCollection<Trade>(kvp.Value)
                    })
                    .ToList();

                TradeGroups = new ObservableCollection<TradeGroup>(groups);
                TotalTradeCount = betsDict.Values.Sum(list => list.Count);
                GameCount = betsDict.Count;
                StatusMessage = $"Showing {TotalTradeCount} trades across {GameCount} games";
            }
            else
            {
                // Filter to specific game
                var normalizedFilter = new Trade { Game = CurrentFilterGame }.NormalizedGame;
                if (betsDict.TryGetValue(normalizedFilter, out var markets))
                {
                    TradeGroups = new ObservableCollection<TradeGroup>
                    {
                        new TradeGroup
                        {
                            GameName = normalizedFilter,
                            Trades = new ObservableCollection<Trade>(markets)
                        }
                    };
                    TotalTradeCount = markets.Count;
                    GameCount = 1;
                    StatusMessage = $"Showing {markets.Count} trade(s) for: {normalizedFilter}";
                }
                else
                {
                    TradeGroups = new ObservableCollection<TradeGroup>();
                    TotalTradeCount = 0;
                    GameCount = 0;
                    StatusMessage = $"No trades found for: {normalizedFilter}";
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading trades");
            StatusMessage = $"Error loading trades: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteTrade(Trade trade)
    {
        try
        {
            await _tradeStorage.RemoveTradeAsync(trade);
            StatusMessage = $"Deleted: {trade.Game} | {trade.Market}";
            await LoadTradesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting trade");
            StatusMessage = $"Error deleting trade: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ClearFilter()
    {
        CurrentFilterGame = null;
        await LoadTradesAsync();
        StatusMessage = "Filter cleared - showing all trades.";
    }

    [RelayCommand]
    private async Task RunF1Macro()
    {
        try
        {
            await Task.Run(async () => await _macroService.RunF1MacroAsync());
            // Clear filter after F1
            await ClearFilter();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error running F1 macro");
            StatusMessage = $"F1 Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RunF2Macro()
    {
        try
        {
            await Task.Run(async () => await _macroService.RunF2MacroAsync());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error running F2 macro");
            StatusMessage = $"F2 Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RunF3Macro()
    {
        try
        {
            await Task.Run(async () => await _macroService.RunF3MacroAsync());
            // Clear filter after F3
            await ClearFilter();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error running F3 macro");
            StatusMessage = $"F3 Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenCoordinateEditor(Window parentWindow)
    {
        try
        {
            var editorViewModel = new CoordinateEditorViewModel(_coordService);
            var editorWindow = new CoordinateEditorView
            {
                DataContext = editorViewModel
            };

            await editorWindow.ShowDialog(parentWindow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error opening coordinate editor");
            StatusMessage = $"Error opening editor: {ex.Message}";
        }
    }
}


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
using Tracker.Avalonia.Services.Input;
using Tracker.Avalonia.Views;
using Serilog;

namespace Tracker.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ITradeStorageService _tradeStorage;
    private readonly IMacroService _macroService;
    private readonly CoordinateConfigService _coordService;
    private readonly IGlobalHotkeyService _hotkeyService;

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

    [ObservableProperty]
    private bool _isTopmost = true;

    [ObservableProperty]
    private int _windowX = 0;

    [ObservableProperty]
    private int _windowY = 0;

    [ObservableProperty]
    private int _windowWidth = 315;

    [ObservableProperty]
    private int _windowHeight = 1000;

    [ObservableProperty]
    private bool _isTitleBarVisible = true;

    private int _savedWindowX;
    private int _savedWindowY;
    private int _savedWindowWidth;
    private int _savedWindowHeight;

    public MainWindowViewModel(
        ITradeStorageService betStorage,
        IMacroService macroService,
        CoordinateConfigService coordService,
        IGlobalHotkeyService hotkeyService)
    {
        _tradeStorage = betStorage;
        _macroService = macroService;
        _coordService = coordService;
        _hotkeyService = hotkeyService;

        _tradeStorage.TradesChanged += OnTradesChanged;
        _macroService.StatusUpdated += OnMacroStatusUpdated;
        _macroService.WindowMoveRequested += OnWindowMoveRequested;
        _macroService.WindowRestoreRequested += OnWindowRestoreRequested;
        _macroService.FilterGameRequested += OnFilterGameRequested;
        _hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;

        // Load initial trades
        Task.Run(async () => await LoadTradesAsync());
    }

    private void OnWindowMoveRequested(object? sender, WindowMoveEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Save current position and size
            _savedWindowX = WindowX;
            _savedWindowY = WindowY;
            _savedWindowWidth = WindowWidth;
            _savedWindowHeight = WindowHeight;

            // Hide title bar
            IsTitleBarVisible = false;

            // Move and resize
            WindowX = e.X;
            WindowY = e.Y;
            WindowWidth = e.Width;
            WindowHeight = e.Height;

            Log.Information("Moved window to ({X},{Y}) with size {W}x{H}", e.X, e.Y, e.Width, e.Height);
        });
    }

    private void OnWindowRestoreRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Restore original position and size
            WindowX = _savedWindowX;
            WindowY = _savedWindowY;
            WindowWidth = _savedWindowWidth;
            WindowHeight = _savedWindowHeight;

            // Show title bar again
            IsTitleBarVisible = true;

            Log.Information("Restored window to ({X},{Y}) with size {W}x{H}", _savedWindowX, _savedWindowY, _savedWindowWidth, _savedWindowHeight);
        });
    }

    private void OnFilterGameRequested(object? sender, string gameName)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await SetFilterAsync(gameName);
        });
    }

    private void OnGlobalHotkeyPressed(object? sender, HotkeyEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                switch (e.HotkeyId)
                {
                    case 1: // F1
                        await RunF1MacroCommand.ExecuteAsync(null);
                        break;
                    case 2: // F2
                        await RunF2MacroCommand.ExecuteAsync(null);
                        break;
                    case 3: // F3
                        await RunF3MacroCommand.ExecuteAsync(null);
                        break;
                    case 4: // Escape
                        await ClearFilterCommand.ExecuteAsync(null);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling global hotkey");
            }
        });
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
    private async Task EditTrade(Trade trade)
    {
        try
        {
            // Temporarily disable Topmost so the dialog can appear on top
            IsTopmost = false;

            var editorViewModel = new TradeEditorViewModel(trade, async (oldTrade, newTrade) =>
            {
                await _tradeStorage.UpdateTradeAsync(oldTrade, newTrade);
                StatusMessage = $"Updated: {newTrade.Game} | {newTrade.Market}";
                await LoadTradesAsync();
            });

            var editorWindow = new Views.TradeEditorDialog
            {
                DataContext = editorViewModel
            };

            // Get the main window from application lifetime
            if (global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                await editorWindow.ShowDialog(desktop.MainWindow);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error editing trade");
            StatusMessage = $"Error editing trade: {ex.Message}";
        }
        finally
        {
            // Re-enable Topmost after dialog closes
            IsTopmost = true;
        }
    }

    [RelayCommand]
    private async Task ClearFilter()
    {
        CurrentFilterGame = null;
        await LoadTradesAsync();
        StatusMessage = "Filter cleared - showing all trades.";
    }

    public async Task SetFilterAsync(string gameName)
    {
        CurrentFilterGame = gameName;
        await LoadTradesAsync();
    }

    [RelayCommand]
    private async Task ClearAllTrades()
    {
        try
        {
            var result = await ShowConfirmDialog("Clear All Trades", "Are you sure you want to delete ALL trades? This cannot be undone.");
            if (result)
            {
                await _tradeStorage.ClearAllTradesAsync();
                CurrentFilterGame = null;
                await LoadTradesAsync();
                StatusMessage = "All trades cleared.";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error clearing all trades");
            StatusMessage = $"Error clearing trades: {ex.Message}";
        }
    }

    private async Task<bool> ShowConfirmDialog(string title, string message)
    {
        // Simple confirmation - in a real app you'd use a proper dialog
        // For now, we'll just return true and let the user be careful with the button
        await Task.CompletedTask;
        return true; // TODO: Implement proper confirmation dialog
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
            // Temporarily disable Topmost so the dialog can appear on top
            IsTopmost = false;

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
        finally
        {
            // Re-enable Topmost after dialog closes
            IsTopmost = true;
        }
    }
}


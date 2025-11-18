using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tracker.Avalonia.Models;
using Serilog;

namespace Tracker.Avalonia.ViewModels;

public partial class TradeEditorViewModel : ViewModelBase
{
    private readonly Trade _originalTrade;
    private readonly Action<Trade, Trade> _onSave;

    [ObservableProperty]
    private string _gameName = string.Empty;

    [ObservableProperty]
    private string _market = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    public TradeEditorViewModel(Trade trade, Action<Trade, Trade> onSave)
    {
        _originalTrade = trade;
        _onSave = onSave;
        
        // Initialize with current values
        GameName = trade.Game;
        Market = trade.Market;
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            // Validate
            if (string.IsNullOrWhiteSpace(GameName))
            {
                ErrorMessage = "Game name cannot be empty";
                HasError = true;
                return;
            }

            if (!GameName.Contains("@"))
            {
                ErrorMessage = "Game name must contain '@' (e.g., Team A @ Team B)";
                HasError = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(Market))
            {
                ErrorMessage = "Market cannot be empty";
                HasError = true;
                return;
            }

            // Create updated trade
            var updatedTrade = new Trade
            {
                Game = GameName.Trim(),
                Market = Market.Trim()
            };

            // Call the save callback
            _onSave(_originalTrade, updatedTrade);

            // Close dialog
            if (global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.MainWindow?.OwnedWindows.FirstOrDefault(w => w is Views.TradeEditorDialog);
                (window as Views.TradeEditorDialog)?.Close(true);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving trade");
            ErrorMessage = $"Error: {ex.Message}";
            HasError = true;
        }
    }

    partial void OnGameNameChanged(string value)
    {
        if (HasError)
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }
    }

    partial void OnMarketChanged(string value)
    {
        if (HasError)
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }
    }
}


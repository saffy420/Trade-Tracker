using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Tracker.Avalonia.Models;

public partial class TradeGroup : ObservableObject
{
    [ObservableProperty]
    private string _gameName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Trade> _trades = new();

    public int TradeCount => Trades.Count;

    partial void OnTradesChanged(ObservableCollection<Trade> value)
    {
        OnPropertyChanged(nameof(TradeCount));
    }
}


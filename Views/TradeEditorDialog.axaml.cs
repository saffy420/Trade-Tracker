using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Tracker.Avalonia.Views;

public partial class TradeEditorDialog : Window
{
    public TradeEditorDialog()
    {
        InitializeComponent();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}


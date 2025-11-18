using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Tracker.Avalonia.Views;

public partial class CoordinateEditorView : Window
{
    public CoordinateEditorView()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}


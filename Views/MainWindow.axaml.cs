using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Tracker.Avalonia.ViewModels;

namespace Tracker.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Handle global key events for F1, F2, F3, Escape
        KeyDown += OnKeyDown;
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        switch (e.Key)
        {
            case Key.F1:
                e.Handled = true;
                await viewModel.RunF1MacroCommand.ExecuteAsync(null);
                break;
            case Key.F2:
                e.Handled = true;
                await viewModel.RunF2MacroCommand.ExecuteAsync(null);
                break;
            case Key.F3:
                e.Handled = true;
                await viewModel.RunF3MacroCommand.ExecuteAsync(null);
                break;
            case Key.Escape:
                e.Handled = true;
                await viewModel.ClearFilterCommand.ExecuteAsync(null);
                break;
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}


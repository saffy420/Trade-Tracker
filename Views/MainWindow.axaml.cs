using System;
using System.ComponentModel;
using Avalonia;
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
        
        // Set initial position
        Position = new PixelPoint(0, 0);
        
        // Subscribe to DataContext changes
        DataContextChanged += OnDataContextChanged;
        
        // Note: Global hotkeys are now handled by the GlobalHotkeyService
        // which works even when the window doesn't have focus
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        if (e.PropertyName == nameof(MainWindowViewModel.WindowX) || 
            e.PropertyName == nameof(MainWindowViewModel.WindowY))
        {
            Position = new PixelPoint(viewModel.WindowX, viewModel.WindowY);
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


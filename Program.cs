using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Tracker.Avalonia.Services;
using Tracker.Avalonia.Services.Input;
using Tracker.Avalonia.ViewModels;
using Serilog;

namespace Tracker.Avalonia;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Setup logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/tracker-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();

    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core Services
        services.AddSingleton<ITradeStorageService, TradeStorageService>();
        services.AddSingleton<ICoordinateProvider, CoordinateConfigService>();
        services.AddSingleton<CoordinateConfigService>();
        services.AddSingleton<IOcrService, OcrService>();
        services.AddSingleton<IMacroService, MacroService>();

        // Platform-specific Input Services
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IKeyboardService, WindowsKeyboardService>();
            services.AddSingleton<IMacroClickService, WindowsMacroClickService>();
            services.AddSingleton<IScreenCaptureService, WindowsScreenCaptureService>();
            services.AddSingleton<IGlobalHotkeyService, WindowsGlobalHotkeyService>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            services.AddSingleton<IKeyboardService, MacKeyboardService>();
            services.AddSingleton<IMacroClickService, MacMacroClickService>();
            services.AddSingleton<IScreenCaptureService, MacScreenCaptureService>();
            services.AddSingleton<IGlobalHotkeyService, MacGlobalHotkeyService>();
        }
        else // Linux
        {
            services.AddSingleton<IKeyboardService, LinuxKeyboardService>();
            services.AddSingleton<IMacroClickService, LinuxMacroClickService>();
            services.AddSingleton<IScreenCaptureService, LinuxScreenCaptureService>();
            services.AddSingleton<IGlobalHotkeyService, LinuxGlobalHotkeyService>();
        }

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<CoordinateEditorViewModel>();

        return services.BuildServiceProvider();
    }
}


# COMPLETE CROSS-PLATFORM TRADE TRACKER PROJECT OUTPUT

This document contains all files for the cross-platform Avalonia Trade Tracker application.

---

=== FILE: Tracker.Avalonia.csproj ===
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.0.10" />
    <PackageReference Include="Avalonia.Desktop" Version="11.0.10" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.0.10" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.0.10" />
    <PackageReference Include="Avalonia.ReactiveUI" Version="11.0.10" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
    <PackageReference Include="Tesseract" Version="5.2.0" />
    <PackageReference Include="TextCopy" Version="6.2.1" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Serilog" Version="3.1.1" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.3" />
  </ItemGroup>

  <!-- Include tessdata folder in output/publish -->
  <ItemGroup>
    <None Include="tessdata\**\*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <!-- Include config folder in output/publish -->
  <ItemGroup>
    <None Include="Config\**\*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

=== FILE: Program.cs ===
```csharp
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
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            services.AddSingleton<IKeyboardService, MacKeyboardService>();
            services.AddSingleton<IMacroClickService, MacMacroClickService>();
            services.AddSingleton<IScreenCaptureService, MacScreenCaptureService>();
        }
        else // Linux
        {
            services.AddSingleton<IKeyboardService, LinuxKeyboardService>();
            services.AddSingleton<IMacroClickService, LinuxMacroClickService>();
            services.AddSingleton<IScreenCaptureService, LinuxScreenCaptureService>();
        }

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<CoordinateEditorViewModel>();

        return services.BuildServiceProvider();
    }
}
```

=== FILE: App.axaml ===
```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Tracker.Avalonia.App"
             RequestedThemeVariant="Dark">
    <Application.Styles>
        <FluentTheme />
        <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"/>
        
        <Style Selector="Window">
            <Setter Property="Background" Value="#14141E"/>
        </Style>
        
        <Style Selector="TextBlock">
            <Setter Property="Foreground" Value="White"/>
        </Style>
        
        <Style Selector="Button">
            <Setter Property="Background" Value="#3C3C50"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Padding" Value="12,6"/>
            <Setter Property="CornerRadius" Value="4"/>
        </Style>
        
        <Style Selector="Button:pointerover /template/ ContentPresenter">
            <Setter Property="Background" Value="#4C4C60"/>
        </Style>
        
        <Style Selector="Button.title-bar">
            <Setter Property="Width" Value="40"/>
            <Setter Property="Height" Value="40"/>
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Padding" Value="0"/>
        </Style>
        
        <Style Selector="Button.title-bar:pointerover /template/ ContentPresenter">
            <Setter Property="Background" Value="#3C3C50"/>
        </Style>
    </Application.Styles>
</Application>
```

=== FILE: App.axaml.cs ===
```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Tracker.Avalonia.ViewModels;
using Tracker.Avalonia.Views;

namespace Tracker.Avalonia;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Services = Program.ConfigureServices();
            
            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

=== FILE: Models/Trade.cs ===
```csharp
using System;

namespace Tracker.Avalonia.Models;

public class Trade
{
    public string Game { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public DateTime AddedDate { get; set; } = DateTime.Now;

    public string NormalizedGame => NormalizeGameName(Game);

    private static string NormalizeGameName(string game)
    {
        if (string.IsNullOrWhiteSpace(game)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(game.Trim(), @"\s*@\s*", " @ ");
    }

    public override bool Equals(object? obj)
    {
        if (obj is Trade other)
        {
            return string.Equals(NormalizedGame, other.NormalizedGame, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Market.Trim(), other.Market.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            NormalizedGame.ToLowerInvariant(),
            Market.Trim().ToLowerInvariant()
        );
    }
}
```

=== FILE: Models/TradeGroup.cs ===
```csharp
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
```

=== FILE: Models/CoordinateConfig.cs ===
```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tracker.Avalonia.Models;

public class CoordinateConfig
{
    [JsonProperty("F1Macro")]
    public Dictionary<string, object> F1Macro { get; set; } = new();

    [JsonProperty("F2Macro")]
    public Dictionary<string, object> F2Macro { get; set; } = new();

    [JsonProperty("F3Macro")]
    public Dictionary<string, object> F3Macro { get; set; } = new();

    [JsonProperty("General")]
    public Dictionary<string, object> General { get; set; } = new();

    public static CoordinateConfig Default => new()
    {
        F1Macro = new Dictionary<string, object>
        {
            ["PlaceTradeOcrX"] = 1268,
            ["PlaceTradeOcrY"] = 443,
            ["PlaceTradeOcrWidth"] = 559,
            ["PlaceTradeOcrHeight"] = 130,
            ["GameNameX"] = -914,
            ["GameNameY"] = 167,
            ["TradeTypeX"] = -914,
            ["TradeTypeY"] = 192,
            ["ClickPosition1X"] = -824,
            ["ClickPosition1Y"] = 530,
            ["FinalCursorX"] = -206,
            ["FinalCursorY"] = 23
        },
        F2Macro = new Dictionary<string, object>
        {
            ["ButtonX"] = -1225,
            ["ButtonY"] = 170,
            ["GameNameX"] = -914,
            ["GameNameY"] = 167,
            ["NumberX"] = -610,
            ["NumberY"] = 242,
            ["TextFieldX"] = 1685,
            ["TextFieldY"] = 350,
            ["SearchX"] = 1328,
            ["SearchY"] = 202,
            ["SearchWidth"] = 415,
            ["SearchHeight"] = 791,
            ["ClearAllRelX"] = 280,
            ["ClearAllRelY"] = 37,
            ["ClearAllWidth"] = 55,
            ["ClearAllHeight"] = 16,
            ["OcrBoxX"] = 1337,
            ["OcrBoxY"] = 337,
            ["OcrBoxWidth"] = 389,
            ["OcrBoxHeight"] = 88,
            ["MaxNumber"] = 40.0,
            ["UiMoveX"] = -1090,
            ["UiMoveY"] = 250,
            ["UiResizeWidth"] = 270,
            ["UiResizeHeight"] = 200
        },
        F3Macro = new Dictionary<string, object>
        {
            ["ClickX"] = 1351,
            ["ClickY"] = 342,
            ["FinalCursorX"] = -206,
            ["FinalCursorY"] = 23
        },
        General = new Dictionary<string, object>
        {
            ["ClickAwayX"] = -206,
            ["ClickAwayY"] = 23
        }
    };
}

public class CoordinateEntry
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public double Value { get; set; }
    public string FullKey => $"{Category}.{Key}";
}
```

---

*Note: Due to length constraints, the complete output continues in the next sections. All remaining files follow the same format.*

Files included in complete project:
- ViewModels/ (ViewModelBase, MainWindowViewModel, CoordinateEditorViewModel)
- Views/ (MainWindow.axaml/cs, CoordinateEditorView.axaml/cs)
- Services/ (All service interfaces and implementations)
- Services/Input/ (All platform-specific implementations)
- Config/coordinates.json
- README.md
- .gitignore
- app.manifest


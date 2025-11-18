using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tracker.Avalonia.Models;
using Tracker.Avalonia.Services;
using Serilog;

namespace Tracker.Avalonia.ViewModels;

public partial class CoordinateEditorViewModel : ViewModelBase
{
    private readonly CoordinateConfigService _coordService;

    [ObservableProperty]
    private ObservableCollection<CoordinateEntry> _coordinates = new();

    [ObservableProperty]
    private CoordinateEntry? _selectedCoordinate;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _searchText = string.Empty;

    public CoordinateEditorViewModel(CoordinateConfigService coordService)
    {
        _coordService = coordService;
        LoadCoordinates();
    }

    private void LoadCoordinates()
    {
        try
        {
            var config = _coordService.GetCurrentConfig();
            var entries = new ObservableCollection<CoordinateEntry>();

            // Load all coordinates from all categories
            AddEntriesFromDict(entries, "F1Macro", config.F1Macro);
            AddEntriesFromDict(entries, "F2Macro", config.F2Macro);
            AddEntriesFromDict(entries, "F3Macro", config.F3Macro);
            AddEntriesFromDict(entries, "General", config.General);

            Coordinates = entries;
            StatusMessage = $"Loaded {entries.Count} coordinates";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading coordinates");
            StatusMessage = $"Error loading: {ex.Message}";
        }
    }

    private void AddEntriesFromDict(ObservableCollection<CoordinateEntry> entries, string category, System.Collections.Generic.Dictionary<string, object> dict)
    {
        foreach (var kvp in dict.OrderBy(k => k.Key))
        {
            entries.Add(new CoordinateEntry
            {
                Category = category,
                Key = kvp.Key,
                Value = Convert.ToDouble(kvp.Value)
            });
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            // Rebuild config from entries
            var config = new CoordinateConfig();

            foreach (var entry in Coordinates)
            {
                var dict = entry.Category switch
                {
                    "F1Macro" => config.F1Macro,
                    "F2Macro" => config.F2Macro,
                    "F3Macro" => config.F3Macro,
                    "General" => config.General,
                    _ => null
                };

                if (dict != null)
                {
                    dict[entry.Key] = entry.Value;
                }
            }

            await _coordService.SaveConfigAsync(config);
            StatusMessage = "✅ Coordinates saved successfully";
            Log.Information("Coordinates saved");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving coordinates");
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddCoordinate()
    {
        try
        {
            var newEntry = new CoordinateEntry
            {
                Category = "General",
                Key = "NewCoordinate",
                Value = 0
            };
            Coordinates.Add(newEntry);
            SelectedCoordinate = newEntry;
            StatusMessage = "Added new coordinate - remember to save!";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error adding coordinate");
            StatusMessage = $"Error adding: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DeleteCoordinate(CoordinateEntry? entry)
    {
        try
        {
            if (entry != null && Coordinates.Contains(entry))
            {
                Coordinates.Remove(entry);
                StatusMessage = $"Deleted {entry.FullKey} - remember to save!";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting coordinate");
            StatusMessage = $"Error deleting: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TestCoordinate(CoordinateEntry? entry)
    {
        try
        {
            if (entry != null)
            {
                // This would require access to the mouse service
                // For now, just show a message
                StatusMessage = $"Test click would occur at value: {entry.Value} for {entry.FullKey}";
                await Task.Delay(1000);
                StatusMessage = "Ready";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error testing coordinate");
            StatusMessage = $"Error testing: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            LoadCoordinates();
            return;
        }

        var filtered = Coordinates
            .Where(c => c.Key.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       c.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Coordinates = new ObservableCollection<CoordinateEntry>(filtered);
        StatusMessage = $"Found {filtered.Count} matches";
    }

    [RelayCommand]
    private async Task Reload()
    {
        try
        {
            await _coordService.ReloadAsync();
            LoadCoordinates();
            StatusMessage = "✅ Reloaded from file";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reloading coordinates");
            StatusMessage = $"Error reloading: {ex.Message}";
        }
    }
}


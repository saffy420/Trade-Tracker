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
    private ObservableCollection<CoordinateEntry> _allCoordinates = new(); // Full list (not filtered)

    [ObservableProperty]
    private ObservableCollection<CoordinateEntry> _coordinates = new(); // Displayed list (may be filtered)

    [ObservableProperty]
    private CoordinateEntry? _selectedCoordinate;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _configFilePath = string.Empty;

    public CoordinateEditorViewModel(CoordinateConfigService coordService)
    {
        _coordService = coordService;
        ConfigFilePath = coordService.GetConfigFilePath();
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

            _allCoordinates = entries;
            Coordinates = new ObservableCollection<CoordinateEntry>(entries); // Show all by default
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
            Log.Information("Starting coordinate save operation with {Count} total entries", _allCoordinates.Count);

            // Rebuild config from ALL entries (not just filtered/displayed ones)
            var config = new CoordinateConfig();

            foreach (var entry in _allCoordinates)
            {
                Log.Debug("Processing entry: {Category}.{Key} = {Value}", entry.Category, entry.Key, entry.Value);

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
                else
                {
                    Log.Warning("Unknown category: {Category}", entry.Category);
                }
            }

            Log.Information("Built config with F1Macro={F1Count}, F2Macro={F2Count}, F3Macro={F3Count}, General={GCount}",
                config.F1Macro.Count, config.F2Macro.Count, config.F3Macro.Count, config.General.Count);

            await _coordService.SaveConfigAsync(config);
            StatusMessage = "✅ Coordinates saved successfully";
            Log.Information("Coordinates saved successfully to file");
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
            
            // Add to both the full list and the displayed list
            _allCoordinates.Add(newEntry);
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
            if (entry != null)
            {
                // Remove from both the full list and the displayed list
                if (_allCoordinates.Contains(entry))
                {
                    _allCoordinates.Remove(entry);
                }
                if (Coordinates.Contains(entry))
                {
                    Coordinates.Remove(entry);
                }
                StatusMessage = $"Deleted {entry.FullKey} - remember to save!";
                Log.Information("Deleted coordinate: {FullKey}", entry.FullKey);
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
            // Show all coordinates when search is cleared
            Coordinates = new ObservableCollection<CoordinateEntry>(_allCoordinates);
            StatusMessage = $"Showing all {_allCoordinates.Count} coordinates";
            return;
        }

        // Always search from the full list, not the currently displayed list
        var filtered = _allCoordinates
            .Where(c => c.Key.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       c.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Coordinates = new ObservableCollection<CoordinateEntry>(filtered);
        StatusMessage = $"Found {filtered.Count} matches for '{SearchText}'";
    }

    [RelayCommand]
    private async Task Reload()
    {
        try
        {
            await _coordService.ReloadAsync();
            
            // Clear search text and reload all coordinates
            SearchText = string.Empty;
            LoadCoordinates();
            
            StatusMessage = "✅ Reloaded from file";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reloading coordinates");
            StatusMessage = $"Error reloading: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenConfigFolder()
    {
        try
        {
            var folderPath = System.IO.Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(folderPath) && System.IO.Directory.Exists(folderPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
                StatusMessage = $"Opened folder: {folderPath}";
                Log.Information("Opened config folder: {Path}", folderPath);
            }
            else
            {
                StatusMessage = "Config folder not found";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error opening config folder");
            StatusMessage = $"Error opening folder: {ex.Message}";
        }
    }
}


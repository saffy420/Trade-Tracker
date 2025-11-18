using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tracker.Avalonia.Models;
using Serilog;

namespace Tracker.Avalonia.Services;

public class CoordinateConfigService : ICoordinateProvider, IDisposable
{
    private readonly string _configFilePath;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly SemaphoreSlim _configLock = new(1, 1);
    private CoordinateConfig _config;

    public event EventHandler? CoordinatesChanged;

    public CoordinateConfigService()
    {
        var configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        _configFilePath = Path.Combine(configDir, "coordinates.json");
        _config = CoordinateConfig.Default;

        // Create default config if it doesn't exist
        if (!File.Exists(_configFilePath))
        {
            SaveConfigToFile(_config);
        }
        else
        {
            LoadConfigFromFile();
        }

        // Setup file watcher for live reload
        _fileWatcher = new FileSystemWatcher(configDir)
        {
            Filter = "coordinates.json",
            NotifyFilter = NotifyFilters.LastWrite
        };
        _fileWatcher.Changed += OnConfigFileChanged;
        _fileWatcher.EnableRaisingEvents = true;

        Log.Information("CoordinateConfigService initialized with file: {FilePath}", _configFilePath);
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        Thread.Sleep(100); // Debounce
        LoadConfigFromFile();
        CoordinatesChanged?.Invoke(this, EventArgs.Empty);
        Log.Information("Coordinates reloaded from file");
    }

    private void LoadConfigFromFile()
    {
        try
        {
            var json = File.ReadAllText(_configFilePath);
            var config = JsonConvert.DeserializeObject<CoordinateConfig>(json);
            if (config != null)
            {
                _config = config;
                Log.Debug("Loaded coordinate config from file");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading coordinate config, using defaults");
            _config = CoordinateConfig.Default;
        }
    }

    private void SaveConfigToFile(CoordinateConfig config)
    {
        try
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);
            Log.Information("Saved coordinate config to file");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving coordinate config");
        }
    }

    public int GetInt(string category, string key, int defaultValue = 0)
    {
        try
        {
            var dict = GetCategoryDict(category);
            if (dict != null && dict.TryGetValue(key, out var value))
            {
                return Convert.ToInt32(value);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error getting int coordinate {Category}.{Key}", category, key);
        }
        return defaultValue;
    }

    public double GetDouble(string category, string key, double defaultValue = 0.0)
    {
        try
        {
            var dict = GetCategoryDict(category);
            if (dict != null && dict.TryGetValue(key, out var value))
            {
                return Convert.ToDouble(value);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error getting double coordinate {Category}.{Key}", category, key);
        }
        return defaultValue;
    }

    private Dictionary<string, object>? GetCategoryDict(string category)
    {
        return category switch
        {
            "F1Macro" => _config.F1Macro,
            "F2Macro" => _config.F2Macro,
            "F3Macro" => _config.F3Macro,
            "General" => _config.General,
            _ => null
        };
    }

    public async Task ReloadAsync()
    {
        await _configLock.WaitAsync();
        try
        {
            LoadConfigFromFile();
            CoordinatesChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _configLock.Release();
        }
    }

    public async Task SaveConfigAsync(CoordinateConfig config)
    {
        await _configLock.WaitAsync();
        try
        {
            _config = config;
            SaveConfigToFile(config);
        }
        finally
        {
            _configLock.Release();
        }
    }

    public CoordinateConfig GetCurrentConfig()
    {
        return _config;
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
        _configLock?.Dispose();
    }
}


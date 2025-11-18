using System;
using System.Threading.Tasks;

namespace Tracker.Avalonia.Services;

public interface ICoordinateProvider
{
    event EventHandler? CoordinatesChanged;
    int GetInt(string category, string key, int defaultValue = 0);
    double GetDouble(string category, string key, double defaultValue = 0.0);
    Task ReloadAsync();
}


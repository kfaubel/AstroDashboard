using System.IO;

namespace AstroDashboard.Services;

public class PathStateService
{
    private readonly string _stateFilePath;

    public PathStateService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "AstroDashboard");
        Directory.CreateDirectory(appFolder);
        _stateFilePath = Path.Combine(appFolder, "last-path.txt");
    }

    public string? GetLastPath()
    {
        try
        {
            if (!File.Exists(_stateFilePath))
            {
                return null;
            }

            var path = File.ReadAllText(_stateFilePath).Trim();
            return Directory.Exists(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }

    public void SaveLastPath(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                File.WriteAllText(_stateFilePath, path);
            }
        }
        catch
        {
            // Ignore state persistence errors.
        }
    }
}

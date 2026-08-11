using System.IO;

namespace AstroDashboard.Services;

public class PathStateService
{
    private readonly string _stateFilePath;
    private readonly string _darkModeStateFilePath;

    public PathStateService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "AstroDashboard");
        Directory.CreateDirectory(appFolder);
        _stateFilePath = Path.Combine(appFolder, "last-path.txt");
        _darkModeStateFilePath = Path.Combine(appFolder, "dark-mode.txt");
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

    public bool? GetDarkModePreference()
    {
        try
        {
            if (!File.Exists(_darkModeStateFilePath))
            {
                return null;
            }

            var value = File.ReadAllText(_darkModeStateFilePath).Trim();
            if (bool.TryParse(value, out var isDarkMode))
            {
                return isDarkMode;
            }
        }
        catch
        {
            // Ignore state persistence errors.
        }

        return null;
    }

    public void SaveDarkModePreference(bool isDarkMode)
    {
        try
        {
            File.WriteAllText(_darkModeStateFilePath, isDarkMode.ToString());
        }
        catch
        {
            // Ignore state persistence errors.
        }
    }
}

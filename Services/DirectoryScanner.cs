using AstroDashboard.Models;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;

namespace AstroDashboard.Services;

public class DirectoryScanner
{
    private const string DataFolderName = "Data";
    private static readonly Regex NightFolderPattern = new(@"^NIGHT_\d{4}-\d{2}-\d{2}$");
    private static readonly Regex FitsFilePattern = new(@"^(\d{4}-\d{2}-\d{2})_\d{2}-\d{2}-\d{2}_(.*?)_.+?_(\d+(?:\.\d+)?)s(?:_|\.)", RegexOptions.IgnoreCase);
    private static readonly Regex RmsPattern = new(@"(?:^|_)RMS_(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
    private static readonly Regex HfrPattern = new(@"(?:^|_)HFR_(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
    
    public class ProjectData
    {
        public string TelescopeName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public List<AstronomyData> Files { get; set; } = new();
    }

    public List<ProjectData> ScanDirectory(string rootPath)
    {
        var results = new List<ProjectData>();
        
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Root directory not found: {rootPath}");
        }

        ScanDirectoryRecursive(rootPath, rootPath, results);
        return results;
    }

    private void ScanDirectoryRecursive(string rootPath, string currentPath, List<ProjectData> results)
    {
        try
        {
            var directories = Directory.GetDirectories(currentPath);
            
            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                
                if (dirName == DataFolderName)
                {
                    // Found a Data folder - extract telescope and project names
                    var projectPath = Path.GetDirectoryName(dir)!;
                    var pathParts = GetPathParts(rootPath, projectPath);
                    
                    var projectData = new ProjectData
                    {
                        TelescopeName = pathParts.Item1,
                        ProjectName = pathParts.Item2,
                        Files = ScanDataFolder(dir)
                    };

                    // Keep the project even if it currently has no parsable files so folder visibility is complete.
                    results.Add(projectData);
                }
                else
                {
                    // Recurse into subdirectories
                    ScanDirectoryRecursive(rootPath, dir, results);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we don't have permission to access
        }
    }

    private (string telescope, string project) GetPathParts(string rootPath, string projectPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, projectPath);
        var parts = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 0)
        {
            return ("Unknown", "Unknown");
        }
        else if (parts.Length == 1)
        {
            return (parts[0], "Default");
        }
        else
        {
            var telescopeName = string.Join(" - ", parts.Take(parts.Length - 1));
            return (telescopeName, parts[^1]);
        }
    }

    private List<AstronomyData> ScanDataFolder(string dataPath)
    {
        var results = new List<AstronomyData>();
        
        try
        {
            var nightDirs = Directory.GetDirectories(dataPath);
            
            foreach (var nightDir in nightDirs)
            {
                var nightName = Path.GetFileName(nightDir);
                
                if (!NightFolderPattern.IsMatch(nightName))
                {
                    continue;
                }
                
                var lightDir = Path.Combine(nightDir, "LIGHT");
                if (!Directory.Exists(lightDir))
                {
                    continue;
                }
                
                try
                {
                    var fitsFiles = Directory.GetFiles(lightDir, "*.fits");
                    
                    foreach (var fitsFile in fitsFiles)
                    {
                        var fileName = Path.GetFileName(fitsFile);
                        var parsed = ParseFitsFileName(fileName);
                        
                        if (parsed != null)
                        {
                            results.Add(parsed);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip if we can't access the LIGHT folder
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip if we can't access the Data folder
        }
        
        return results;
    }

    private AstronomyData? ParseFitsFileName(string fileName)
    {
        var match = FitsFilePattern.Match(fileName);
        
        if (!match.Success)
        {
            return null;
        }
        
        if (!DateTime.TryParse(match.Groups[1].Value, out var date))
        {
            return null;
        }

        if (!TryParseFilter(match.Groups[2].Value, out var filter))
        {
            return null;
        }

        if (!double.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var exposure))
        {
            return null;
        }

        if (!TryParseMetricAfterToken(fileName, RmsPattern, out var rms))
        {
            return null;
        }

        if (!TryParseMetricAfterToken(fileName, HfrPattern, out var hfr))
        {
            return null;
        }
        
        return new AstronomyData
        {
            FileName = fileName,
            Date = date,
            Filter = filter,
            ExposureSeconds = exposure,
            Rms = rms,
            Hfr = hfr
        };
    }

    private static bool TryParseMetricAfterToken(string fileName, Regex pattern, out double value)
    {
        value = 0;
        var metricMatch = pattern.Match(fileName);
        if (!metricMatch.Success)
        {
            return false;
        }

        return double.TryParse(metricMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseFilter(string token, out char filter)
    {
        filter = default;
        var normalized = token.Trim();

        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        if (normalized.Length == 1)
        {
            var single = char.ToUpperInvariant(normalized[0]);
            if (single is 'L' or 'R' or 'G' or 'B' or 'S' or 'H' or 'O')
            {
                filter = single;
                return true;
            }
        }

        switch (normalized.ToUpperInvariant())
        {
            case "RED":
                filter = 'R';
                return true;
            case "GREEN":
                filter = 'G';
                return true;
            case "BLUE":
                filter = 'B';
                return true;
            case "LUMINANCE":
            case "LUM":
                filter = 'L';
                return true;
            case "SII":
            case "S2":
                filter = 'S';
                return true;
            case "HA":
            case "HALPHA":
            case "HYDROGEN":
                filter = 'H';
                return true;
            case "OIII":
            case "O3":
                filter = 'O';
                return true;
            default:
                return false;
        }
    }
}

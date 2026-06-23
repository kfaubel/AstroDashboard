using AstroDashboard.Models;
using System.Text.RegularExpressions;
using System.IO;

namespace AstroDashboard.Services;

public class DirectoryScanner
{
    private const string DataFolderName = "Data";
    private static readonly Regex NightFolderPattern = new(@"^NIGHT_\d{4}-\d{2}-\d{2}$");
    private static readonly Regex FitsFilePattern = new(@"^(\d{4}-\d{2}-\d{2})_\d{2}-\d{2}-\d{2}_([LRGBSHO])_.+?_(\d+(?:\.\d+)?)s_");
    
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
                    
                    if (projectData.Files.Count > 0)
                    {
                        results.Add(projectData);
                    }
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
        
        if (!char.TryParse(match.Groups[2].Value, out var filter))
        {
            return null;
        }
        
        if (!double.TryParse(match.Groups[3].Value, out var exposure))
        {
            return null;
        }
        
        return new AstronomyData
        {
            FileName = fileName,
            Date = date,
            Filter = filter,
            ExposureSeconds = exposure
        };
    }
}

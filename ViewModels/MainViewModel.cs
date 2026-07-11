using AstroDashboard.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;

namespace AstroDashboard.ViewModels;

public class MainViewModel : BaseViewModel
{
    private static readonly char[] FilterDisplayOrder = ['L', 'R', 'G', 'B', 'S', 'H', 'O'];

    private readonly DirectoryScanner _scanner;
    private readonly PathStateService _pathStateService;
    private ObservableCollection<TreeNodeViewModel> _treeNodes;
    private string _statusMessage;
    private ICommand? _browseCommand;
    private ICommand? _openNightInApexAstroCommand;
    private string _selectedPath;
    private List<DirectoryScanner.ProjectData> _allProjects;

    public ObservableCollection<TreeNodeViewModel> TreeNodes
    {
        get => _treeNodes;
        set => SetProperty(ref _treeNodes, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string SelectedPath
    {
        get => _selectedPath;
        set => SetProperty(ref _selectedPath, value);
    }

    public ICommand BrowseCommand => _browseCommand ??= new RelayCommand(_ => BrowseForDirectory());
    public ICommand OpenNightInApexAstroCommand => _openNightInApexAstroCommand ??= new RelayCommand(
        path => OpenNightInApexAstro(path as string),
        path => path is string p && !string.IsNullOrWhiteSpace(p));
    public ICommand RefreshCommand { get; }

    public MainViewModel()
    {
        _scanner = new DirectoryScanner();
        _pathStateService = new PathStateService();
        _treeNodes = new ObservableCollection<TreeNodeViewModel>();
        _statusMessage = "Ready";
        _selectedPath = string.Empty;
        _allProjects = new List<DirectoryScanner.ProjectData>();
        RefreshCommand = new RelayCommand(_ => RefreshCurrentPath());
    }

    public void LoadInitialPath(string? path)
    {
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            SelectedPath = path;
            ScanAndLoad(path);
        }
    }

    private void BrowseForDirectory()
    {
        var selectedPath = FolderDialog.SelectFolder("Select the root directory containing astrophotography data", SelectedPath);
        if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
        {
            SelectedPath = selectedPath;
            ScanAndLoad(selectedPath);
        }
    }

    private void RefreshCurrentPath()
    {
        // Refresh with the currently selected path
        if (!string.IsNullOrEmpty(SelectedPath) && Directory.Exists(SelectedPath))
        {
            ScanAndLoad(SelectedPath);
        }
        else
        {
            StatusMessage = "Please enter a valid directory path";
        }
    }

    private void ScanAndLoad(string path)
    {
        try
        {
            StatusMessage = "Scanning directory...";
            _pathStateService.SaveLastPath(path);
            _allProjects = _scanner.ScanDirectory(path);

            if (_allProjects.Count == 0)
            {
                StatusMessage = "No astrophotography data found.";
                TreeNodes.Clear();
                return;
            }

            BuildTreeStructure();
            StatusMessage = $"Loaded {_allProjects.Count} project(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            TreeNodes.Clear();
        }
    }

    private void BuildTreeStructure()
    {
        TreeNodes.Clear();
        var telescopeGroups = _allProjects
            .GroupBy(p => p.TelescopeName)
            .OrderBy(g => g.Key);

        foreach (var telescopeGroup in telescopeGroups)
        {
            var telescopeNode = new TreeNodeViewModel(telescopeGroup.Key, "Telescope");
            telescopeNode.IsExpanded = true;

            var projectGroups = telescopeGroup
                .GroupBy(p => p.ProjectName)
                .OrderBy(g => g.Key);

            foreach (var projectGroup in projectGroups)
            {
                var projectFiles = projectGroup.SelectMany(p => p.Files).ToList();
                var projectNode = new TreeNodeViewModel(
                    projectGroup.Key,
                    "Project",
                    fileCount: projectFiles.Count,
                    totalExposureMinutes: projectFiles.Sum(f => f.ExposureMinutes),
                    averageRms: projectFiles.Any() ? projectFiles.Average(f => f.Rms) : null,
                    averageHfr: projectFiles.Any() ? projectFiles.Average(f => f.Hfr) : null);
                projectNode.IsExpanded = false;

                // Add project-level filter summary rows first.
                foreach (var filter in FilterDisplayOrder)
                {
                    var filterFiles = projectFiles.Where(f => f.Filter == filter).ToList();
                    var filterSummaryNode = new TreeNodeViewModel(
                        filter.ToString(),
                        "Filter",
                        fileCount: filterFiles.Count,
                        totalExposureMinutes: filterFiles.Sum(f => f.ExposureMinutes),
                        averageRms: filterFiles.Any() ? filterFiles.Average(f => f.Rms) : null,
                        averageHfr: filterFiles.Any() ? filterFiles.Average(f => f.Hfr) : null);
                    projectNode.AddChild(filterSummaryNode);
                }

                var nightGroups = projectFiles
                    .Where(f => !string.IsNullOrWhiteSpace(f.NightFolderPath))
                    .GroupBy(f => f.NightFolderPath)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var nightGroup in nightGroups)
                {
                    var nightFolderPath = nightGroup.Key;
                    var nightFolderName = Path.GetFileName(nightFolderPath);
                    var nightNode = new TreeNodeViewModel(
                        nightFolderName,
                        "Night",
                        associatedData: nightFolderPath,
                        fileCount: nightGroup.Count(),
                        totalExposureMinutes: nightGroup.Sum(f => f.ExposureMinutes),
                        averageRms: nightGroup.Average(f => f.Rms),
                        averageHfr: nightGroup.Average(f => f.Hfr));

                    var filterGroups = nightGroup
                        .GroupBy(f => f.Filter)
                        .OrderBy(g => GetFilterSortOrder(g.Key));

                    foreach (var filterGroup in filterGroups)
                    {
                        var filterNode = new TreeNodeViewModel(
                            filterGroup.Key.ToString(),
                            "Filter",
                            fileCount: filterGroup.Count(),
                            totalExposureMinutes: filterGroup.Sum(f => f.ExposureMinutes),
                            averageRms: filterGroup.Average(f => f.Rms),
                            averageHfr: filterGroup.Average(f => f.Hfr));
                        nightNode.AddChild(filterNode);
                    }

                    projectNode.AddChild(nightNode);
                }

                telescopeNode.AddChild(projectNode);
            }

            TreeNodes.Add(telescopeNode);
        }
    }

    private static int GetFilterSortOrder(char filter)
    {
        var index = Array.IndexOf(FilterDisplayOrder, filter);
        return index >= 0 ? index : 99;
    }

    private void OpenNightInApexAstro(string? nightFolderPath)
    {
        if (string.IsNullOrWhiteSpace(nightFolderPath))
        {
            StatusMessage = "Night folder path is missing.";
            return;
        }

        var nightDirectoryPath = Path.GetFullPath(nightFolderPath);
        var lightDirectoryPath = Path.Combine(nightDirectoryPath, "LIGHT");

        if (!Directory.Exists(nightDirectoryPath))
        {
            StatusMessage = $"Night folder not found: {nightDirectoryPath}";
            return;
        }

        if (!Directory.Exists(lightDirectoryPath))
        {
            StatusMessage = $"LIGHT folder not found: {lightDirectoryPath}";
            return;
        }

        var normalizedLightDirectoryPath = Path.TrimEndingDirectorySeparator(lightDirectoryPath) + Path.DirectorySeparatorChar;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ApexAstro.exe",
                WorkingDirectory = lightDirectoryPath,
                UseShellExecute = true
            };
            startInfo.ArgumentList.Add(normalizedLightDirectoryPath);
            Process.Start(startInfo);
            StatusMessage = $"Opened in ApexAstro: {normalizedLightDirectoryPath}";
            return;
        }
        catch
        {
            // Try non-extension executable name as a fallback.
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ApexAstro",
                WorkingDirectory = lightDirectoryPath,
                UseShellExecute = true
            };
            startInfo.ArgumentList.Add(normalizedLightDirectoryPath);
            Process.Start(startInfo);
            StatusMessage = $"Opened in ApexAstro: {normalizedLightDirectoryPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open ApexAstro: {ex.Message}";
        }
    }
}

using AstroDashboard.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.IO;

namespace AstroDashboard.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly DirectoryScanner _scanner;
    private readonly PathStateService _pathStateService;
    private ObservableCollection<TreeNodeViewModel> _treeNodes;
    private string _statusMessage;
    private ICommand? _browseCommand;
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
                    totalExposureMinutes: projectFiles.Sum(f => f.ExposureMinutes));
                projectNode.IsExpanded = false;

                var filterGroups = projectFiles
                    .GroupBy(f => f.Filter)
                    .OrderBy(g => GetFilterSortOrder(g.Key));

                foreach (var filterGroup in filterGroups)
                {
                    var filterNode = new TreeNodeViewModel(
                        filterGroup.Key.ToString(),
                        "Filter",
                        fileCount: filterGroup.Count(),
                        totalExposureMinutes: filterGroup.Sum(f => f.ExposureMinutes));

                    var nightGroups = filterGroup
                        .GroupBy(f => f.Date.ToString("yyyy-MM-dd"))
                        .OrderBy(g => g.Key);

                    foreach (var nightGroup in nightGroups)
                    {
                        var nightNode = new TreeNodeViewModel(
                            $"NIGHT_{nightGroup.Key}",
                            "Night",
                            associatedData: nightGroup.Key,
                            fileCount: nightGroup.Count(),
                            totalExposureMinutes: nightGroup.Sum(f => f.ExposureMinutes));
                        filterNode.AddChild(nightNode);
                    }

                    projectNode.AddChild(filterNode);
                }

                telescopeNode.AddChild(projectNode);
            }

            TreeNodes.Add(telescopeNode);
        }
    }

    private static int GetFilterSortOrder(char filter)
    {
        return filter switch
        {
            'L' => 0,
            'R' => 1,
            'G' => 2,
            'B' => 3,
            'S' => 4,
            'H' => 5,
            'O' => 6,
            _ => 99
        };
    }
}

using AstroDashboard.Services;
using AstroDashboard.Models;
using AstroDashboard.Views;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Globalization;
using Microsoft.Win32;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace AstroDashboard.ViewModels;

public class MainViewModel : BaseViewModel
{
    private static readonly char[] FilterDisplayOrder = ['L', 'R', 'G', 'B', 'S', 'H', 'O'];
    private const string ApexAstroReviewedFileName = ".apexastro.reviewed.json";
    private const string ApexAstroSnrFileName = ".apexastro.snr.json";

    private readonly DirectoryScanner _scanner;
    private readonly PathStateService _pathStateService;
    private ObservableCollection<TreeNodeViewModel> _treeNodes;
    private ObservableCollection<TreeNodeViewModel> _visibleTreeNodes;
    private string _statusMessage;
    private ICommand? _browseCommand;
    private ICommand? _calculateSnrCommand;
    private ICommand? _openNightInApexAstroCommand;
    private ICommand? _toggleNightReviewCommand;
    private ICommand? _toggleNodeExpansionCommand;
    private bool _isDarkModeEnabled;
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

    public ObservableCollection<TreeNodeViewModel> VisibleTreeNodes
    {
        get => _visibleTreeNodes;
        set => SetProperty(ref _visibleTreeNodes, value);
    }

    public string SelectedPath
    {
        get => _selectedPath;
        set => SetProperty(ref _selectedPath, value);
    }

    public bool IsDarkModeEnabled
    {
        get => _isDarkModeEnabled;
        set
        {
            if (_isDarkModeEnabled != value)
            {
                SetProperty(ref _isDarkModeEnabled, value);
                ApplyThemeResources(_isDarkModeEnabled);
                _pathStateService.SaveDarkModePreference(_isDarkModeEnabled);
            }
        }
    }

    public ICommand BrowseCommand => _browseCommand ??= new RelayCommand(_ => BrowseForDirectory());
    public ICommand CalculateSnrCommand => _calculateSnrCommand ??= new RelayCommand(_ => CalculateSnrForSelectedFolderAsync());
    public ICommand ToggleNodeExpansionCommand => _toggleNodeExpansionCommand ??= new RelayCommand(
        node => ToggleNodeExpansion(node as TreeNodeViewModel),
        node => node is TreeNodeViewModel);
    public ICommand OpenNightInApexAstroCommand => _openNightInApexAstroCommand ??= new RelayCommand(
        path => OpenNightInApexAstro(path as string),
        path => path is string p && !string.IsNullOrWhiteSpace(p));
    public ICommand ToggleNightReviewCommand => _toggleNightReviewCommand ??= new RelayCommand(
        path => ToggleNightReview(path as string),
        path => path is string p && !string.IsNullOrWhiteSpace(p));
    public ICommand RefreshCommand { get; }

    public MainViewModel()
    {
        _scanner = new DirectoryScanner();
        _pathStateService = new PathStateService();
        _treeNodes = new ObservableCollection<TreeNodeViewModel>();
        _visibleTreeNodes = new ObservableCollection<TreeNodeViewModel>();
        _statusMessage = "Ready";
        _isDarkModeEnabled = _pathStateService.GetDarkModePreference() ?? IsSystemDarkMode();
        _selectedPath = string.Empty;
        _allProjects = new List<DirectoryScanner.ProjectData>();
        RefreshCommand = new RelayCommand(_ => RefreshCurrentPath());
        ApplyThemeResources(_isDarkModeEnabled);
    }

    private static bool IsSystemDarkMode()
    {
        const string personalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        const string appsUseLightTheme = "AppsUseLightTheme";

        try
        {
            using var personalizeKey = Registry.CurrentUser.OpenSubKey(personalizeRegistryPath);
            if (personalizeKey?.GetValue(appsUseLightTheme) is int value)
            {
                return value == 0;
            }
        }
        catch
        {
            // Default to light theme if system preference cannot be read.
        }

        return false;
    }

    private static void SetBrush(IDictionary resources, string key, string colorHex)
    {
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
        if (resources[key] is SolidColorBrush existingBrush && !existingBrush.IsFrozen)
        {
            existingBrush.Color = color;
            return;
        }

        resources[key] = new SolidColorBrush(color);
    }

    public static bool GetPreferredDarkMode(PathStateService? pathStateService = null)
    {
        var service = pathStateService ?? new PathStateService();
        return service.GetDarkModePreference() ?? IsSystemDarkMode();
    }

    public static void ApplyThemeResources(bool useDarkMode)
    {
        var resources = Application.Current.Resources;
        if (useDarkMode)
        {
            SetBrush(resources, "WindowBackgroundBrush", "#0B1220");
            SetBrush(resources, "SurfaceBrush", "#111827");
            SetBrush(resources, "BorderBrush", "#334155");
            SetBrush(resources, "SectionHeaderBrush", "#1F2937");
            SetBrush(resources, "ColumnHeaderBrush", "#172033");
            SetBrush(resources, "ColumnDividerBrush", "#334155");
            SetBrush(resources, "RowDividerBrush", "#263244");
            SetBrush(resources, "PrimaryTextBrush", "#E5E7EB");
            SetBrush(resources, "SecondaryTextBrush", "#CBD5E1");
            SetBrush(resources, "TreeGlyphHoverBrush", "#FFFFFF");

            SetBrush(resources, "PrimaryButtonBackgroundBrush", "#2563EB");
            SetBrush(resources, "PrimaryButtonForegroundBrush", "#F8FAFC");

            SetBrush(resources, "ReviewActionBackgroundBrush", "#1F2937");
            SetBrush(resources, "ReviewActionBorderBrush", "#475569");
            SetBrush(resources, "ReviewActionForegroundBrush", "#E2E8F0");

            SetBrush(resources, "ReviewStatusPendingBrush", "#FBBF24");
            SetBrush(resources, "ReviewStatusReviewedBrush", "#86EFAC");

            SetBrush(resources, "NodeTypeTelescopeBrush", "#93C5FD");
            SetBrush(resources, "NodeTypeProjectBrush", "#6EE7B7");
            SetBrush(resources, "NodeTypeFilterBrush", "#FDBA74");
            SetBrush(resources, "NodeTypeNightBrush", "#F1F5F9");

            SetBrush(resources, "ComboBoxBackgroundBrush", "#111827");
            SetBrush(resources, "ComboBoxBorderBrush", "#475569");
            SetBrush(resources, "ComboBoxForegroundBrush", "#E5E7EB");
            SetBrush(resources, "ComboBoxPopupBackgroundBrush", "#0F172A");
            SetBrush(resources, "ComboBoxItemHoverBrush", "#1E3A8A");
            SetBrush(resources, "ComboBoxItemSelectedBrush", "#1D4ED8");
        }
        else
        {
            SetBrush(resources, "WindowBackgroundBrush", "#EEF2F7");
            SetBrush(resources, "SurfaceBrush", "#FFFFFF");
            SetBrush(resources, "BorderBrush", "#CBD5E1");
            SetBrush(resources, "SectionHeaderBrush", "#E2E8F0");
            SetBrush(resources, "ColumnHeaderBrush", "#F8FAFC");
            SetBrush(resources, "ColumnDividerBrush", "#D9E1EC");
            SetBrush(resources, "RowDividerBrush", "#E2E8F0");
            SetBrush(resources, "PrimaryTextBrush", "#0F172A");
            SetBrush(resources, "SecondaryTextBrush", "#334155");
            SetBrush(resources, "TreeGlyphHoverBrush", "#0B1220");

            SetBrush(resources, "PrimaryButtonBackgroundBrush", "#1D4ED8");
            SetBrush(resources, "PrimaryButtonForegroundBrush", "#F8FAFC");

            SetBrush(resources, "ReviewActionBackgroundBrush", "#E2E8F0");
            SetBrush(resources, "ReviewActionBorderBrush", "#94A3B8");
            SetBrush(resources, "ReviewActionForegroundBrush", "#0F172A");

            SetBrush(resources, "ReviewStatusPendingBrush", "#B45309");
            SetBrush(resources, "ReviewStatusReviewedBrush", "#166534");

            SetBrush(resources, "NodeTypeTelescopeBrush", "#1D4ED8");
            SetBrush(resources, "NodeTypeProjectBrush", "#047857");
            SetBrush(resources, "NodeTypeFilterBrush", "#B45309");
            SetBrush(resources, "NodeTypeNightBrush", "#1E293B");

            SetBrush(resources, "ComboBoxBackgroundBrush", "#FFFFFF");
            SetBrush(resources, "ComboBoxBorderBrush", "#94A3B8");
            SetBrush(resources, "ComboBoxForegroundBrush", "#0F172A");
            SetBrush(resources, "ComboBoxPopupBackgroundBrush", "#FFFFFF");
            SetBrush(resources, "ComboBoxItemHoverBrush", "#DBEAFE");
            SetBrush(resources, "ComboBoxItemSelectedBrush", "#BFDBFE");
        }
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

    private async void CalculateSnrForSelectedFolderAsync()
    {
        const string computeSnrExecutablePath = @"C:\Users\ken\AppData\Local\ApexAstro-win-x64-local\compute-snr.exe";

        if (!File.Exists(computeSnrExecutablePath))
        {
            StatusMessage = "compute-snr executable not found.";
            ThemedMessageDialog.ShowMessage(
                Application.Current.MainWindow,
                "Calculate SNR",
                "Could not find compute-snr.",
                computeSnrExecutablePath);
            return;
        }

        var selectedPath = FolderDialog.SelectFolder("Select a folder to search for LIGHT directories", SelectedPath);
        if (string.IsNullOrWhiteSpace(selectedPath) || !Directory.Exists(selectedPath))
        {
            return;
        }

        StatusMessage = "Searching for LIGHT directories...";
        var lightDirectories = FindLightDirectories(selectedPath).ToList();

        if (lightDirectories.Count == 0)
        {
            StatusMessage = "No LIGHT directories found.";
            ThemedMessageDialog.ShowMessage(
                Application.Current.MainWindow,
                "Calculate SNR",
                "No folders named LIGHT were found under the selected directory.");
            return;
        }

        StatusMessage = "Checking SNR freshness...";
        var outOfDateSnrFolders = FindOutOfDateSnrFolders(lightDirectories).ToList();
        if (outOfDateSnrFolders.Count == 0)
        {
            StatusMessage = "All SNR values are up to date.";
            ThemedMessageDialog.ShowMessage(
                Application.Current.MainWindow,
                "Calculate SNR",
                "All LIGHT folders have up-to-date SNR values.");
            return;
        }

        var previewCount = Math.Min(outOfDateSnrFolders.Count, 50);
        var previewLines = outOfDateSnrFolders
            .Take(previewCount)
            .Select(folder => folder.LightDirectoryPath)
            .ToList();
        var additionalCount = outOfDateSnrFolders.Count - previewCount;
        if (additionalCount > 0)
        {
            previewLines.Add($"...and {additionalCount} more folder(s)");
        }

        var confirmationAccepted = ThemedMessageDialog.ShowConfirmation(
            Application.Current.MainWindow,
            "Calculate SNR",
            "Update SNR value?",
            string.Join(Environment.NewLine, previewLines),
            okButtonText: "Update",
            cancelButtonText: "Cancel");
        if (!confirmationAccepted)
        {
            StatusMessage = "SNR calculation cancelled by user.";
            return;
        }

        var targetLightDirectories = outOfDateSnrFolders
            .Select(folder => folder.LightDirectoryPath)
            .ToList();

        var progressWindow = new SnrProgressWindow
        {
            Owner = Application.Current.MainWindow
        };
        var cancellationTokenSource = new CancellationTokenSource();
        var processLock = new object();
        Process? activeProcess = null;

        void AppendLine(string message)
        {
            progressWindow.Dispatcher.Invoke(() => progressWindow.AppendLine(message));
        }

        progressWindow.CancelRequested += (_, _) =>
        {
            cancellationTokenSource.Cancel();

            Process? processToCancel = null;
            lock (processLock)
            {
                processToCancel = activeProcess;
            }

            if (processToCancel != null)
            {
                TryKillProcess(processToCancel);
            }

            progressWindow.Dispatcher.Invoke(progressWindow.MarkCancelling);
            AppendLine("Cancellation requested...");
        };

        progressWindow.Show();
    AppendLine($"Found {targetLightDirectories.Count} out-of-date LIGHT folder(s) to process.");

        var processedCount = 0;
        var failedCount = 0;
        var wasCancelled = false;

        try
        {
            foreach (var lightDirectory in targetLightDirectories)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    wasCancelled = true;
                    break;
                }

                AppendLine($"Starting folder: {lightDirectory}");

                int exitCode;
                try
                {
                    exitCode = await RunComputeSnrAsync(
                        computeSnrExecutablePath,
                        lightDirectory,
                        AppendLine,
                        cancellationTokenSource.Token,
                        processLock,
                        process => activeProcess = process);
                }
                catch (OperationCanceledException)
                {
                    wasCancelled = true;
                    break;
                }

                processedCount++;
                if (exitCode == 0)
                {
                    AppendLine($"Completed folder: {lightDirectory}");
                }
                else
                {
                    failedCount++;
                    AppendLine($"Failed folder (exit code {exitCode}): {lightDirectory}");
                }

                AppendLine(string.Empty);
            }
        }
        finally
        {
            lock (processLock)
            {
                activeProcess = null;
            }
            cancellationTokenSource.Dispose();

            if (wasCancelled)
            {
                AppendLine("Cancelled.");
                StatusMessage = $"SNR calculation cancelled after {processedCount} folder(s).";
            }
            else
            {
                AppendLine($"Completed. Processed {processedCount} folder(s), failures: {failedCount}.");
                StatusMessage = $"Processed {processedCount} LIGHT director{(processedCount == 1 ? "y" : "ies")}.";
            }

            progressWindow.Dispatcher.Invoke(progressWindow.MarkCompleted);
        }
    }

    private static IEnumerable<OutOfDateSnrFolder> FindOutOfDateSnrFolders(IEnumerable<string> lightDirectories)
    {
        foreach (var lightDirectory in lightDirectories)
        {
            var outOfDateReason = GetOutOfDateSnrReason(lightDirectory);
            if (outOfDateReason != null)
            {
                yield return new OutOfDateSnrFolder(lightDirectory, outOfDateReason);
            }
        }
    }

    private static string? GetOutOfDateSnrReason(string lightDirectory)
    {
        var snrFilePath = Path.Combine(lightDirectory, ApexAstroSnrFileName);
        if (!File.Exists(snrFilePath))
        {
            return "missing SNR file";
        }

        if (!TryReadSnrComputedUtc(snrFilePath, out var computedUtc))
        {
            return "invalid or missing ComputedUtc";
        }

        if (TryGetLatestDataFileTimestampUtc(lightDirectory, out var latestDataFileUtc)
            && latestDataFileUtc > computedUtc)
        {
            return "data newer than SNR";
        }

        return null;
    }

    private static bool TryReadSnrComputedUtc(string snrFilePath, out DateTime computedUtc)
    {
        computedUtc = default;

        try
        {
            var json = File.ReadAllText(snrFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !TryGetPropertyCaseInsensitive(document.RootElement, "ComputedUtc", out var computedElement) ||
                computedElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var computedText = computedElement.GetString();
            if (string.IsNullOrWhiteSpace(computedText))
            {
                return false;
            }

            return DateTime.TryParse(
                computedText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out computedUtc);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryGetLatestDataFileTimestampUtc(string lightDirectory, out DateTime latestUtc)
    {
        latestUtc = DateTime.MinValue;

        try
        {
            var dataFiles = Directory.EnumerateFiles(lightDirectory)
                .Where(path => !Path.GetFileName(path).Equals(ApexAstroSnrFileName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (dataFiles.Count == 0)
            {
                return false;
            }

            latestUtc = dataFiles.Max(File.GetLastWriteTimeUtc);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static async Task<int> RunComputeSnrAsync(
        string executablePath,
        string lightDirectory,
        Action<string> appendLine,
        CancellationToken cancellationToken,
        object processLock,
        Action<Process?> setActiveProcess)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("--snr");
        startInfo.ArgumentList.Add("-d");
        startInfo.ArgumentList.Add(lightDirectory);

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrEmpty(eventArgs.Data))
            {
                appendLine(eventArgs.Data);
            }
        };

        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrEmpty(eventArgs.Data))
            {
                appendLine($"ERR: {eventArgs.Data}");
            }
        };

        if (!process.Start())
        {
            appendLine("Failed to start compute-snr.");
            return -1;
        }

        lock (processLock)
        {
            setActiveProcess(process);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKillProcess(process);
            throw;
        }
        finally
        {
            lock (processLock)
            {
                setActiveProcess(null);
            }
        }

        return process.ExitCode;
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process has already exited.
        }
        catch (NotSupportedException)
        {
            // Process exit handling failed; ignore during cancellation.
        }
        catch (Win32Exception)
        {
            // Process may have already terminated.
        }
    }

    private static IEnumerable<string> FindLightDirectories(string rootPath)
    {
        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            var currentPath = stack.Pop();
            IEnumerable<string> childDirectories;

            try
            {
                childDirectories = Directory.EnumerateDirectories(currentPath);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var childDirectory in childDirectories)
            {
                if (string.Equals(Path.GetFileName(childDirectory), "LIGHT", StringComparison.OrdinalIgnoreCase))
                {
                    yield return childDirectory;
                }

                stack.Push(childDirectory);
            }
        }
    }

    private static string RunComputeSnr(string executablePath, string lightDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("--snr");
        startInfo.ArgumentList.Add("-d");
        startInfo.ArgumentList.Add(lightDirectory);

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return "Failed to start compute-snr.";
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            return stdout;
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            return $"No stdout returned. STDERR:\n{stderr}";
        }

        return $"No output returned. Exit code: {process.ExitCode}";
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
                VisibleTreeNodes.Clear();
                return;
            }

            BuildTreeStructure();
            StatusMessage = $"Loaded {_allProjects.Count} project(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            TreeNodes.Clear();
            VisibleTreeNodes.Clear();
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
                var projectFilterSnrSamples = new Dictionary<char, List<SnrSample>>();
                var projectFilterSubordinateCounts = new Dictionary<char, int>();
                var nightNodes = new List<TreeNodeViewModel>();
                var totalNightCount = 0;
                var nightsWithSnrDataCount = 0;
                var projectNode = new TreeNodeViewModel(
                    projectGroup.Key,
                    "Project",
                    fileCount: projectFiles.Count,
                    totalExposureMinutes: projectFiles.Sum(f => f.ExposureMinutes),
                    averageRms: projectFiles.Any() ? projectFiles.Average(f => f.Rms) : null,
                    averageHfr: projectFiles.Any() ? projectFiles.Average(f => f.Hfr) : null,
                    maxRms: projectFiles.Any() ? projectFiles.Max(f => f.Rms) : null,
                    maxHfr: projectFiles.Any() ? projectFiles.Max(f => f.Hfr) : null);
                projectNode.IsExpanded = false;

                var nightFilterSnrByPath = BuildNightFilterSnrMap(projectFiles);

                var nightGroups = projectFiles
                    .Where(f => !string.IsNullOrWhiteSpace(f.NightFolderPath))
                    .GroupBy(f => f.NightFolderPath)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var nightGroup in nightGroups)
                {
                    totalNightCount++;
                    var nightFolderPath = nightGroup.Key;
                    var nightFolderName = Path.GetFileName(nightFolderPath);
                    var isReviewed = IsNightReviewed(nightFolderPath);
                    if (nightFilterSnrByPath.TryGetValue(nightFolderPath, out var nightSnrData) && nightSnrData.Count > 0)
                    {
                        nightsWithSnrDataCount++;
                    }

                    var nightNode = new TreeNodeViewModel(
                        nightFolderName,
                        "Night",
                        associatedData: nightFolderPath,
                        reviewed: isReviewed,
                        fileCount: nightGroup.Count(),
                        totalExposureMinutes: nightGroup.Sum(f => f.ExposureMinutes),
                        averageRms: nightGroup.Average(f => f.Rms),
                        averageHfr: nightGroup.Average(f => f.Hfr),
                        maxRms: nightGroup.Max(f => f.Rms),
                        maxHfr: nightGroup.Max(f => f.Hfr));

                    var filterGroups = nightGroup
                        .GroupBy(f => f.Filter)
                        .OrderBy(g => GetFilterSortOrder(g.Key));

                    foreach (var filterGroup in filterGroups)
                    {
                        var filterKey = filterGroup.Key;
                        if (!projectFilterSubordinateCounts.TryGetValue(filterKey, out var subordinateCount))
                        {
                            subordinateCount = 0;
                        }
                        projectFilterSubordinateCounts[filterKey] = subordinateCount + 1;

                        double? nightSnr = null;
                        SnrSample? snrSample = null;
                        if (TryGetNightFilterSnr(nightFilterSnrByPath, nightFolderPath, filterKey, out var foundSample))
                        {
                            snrSample = foundSample;
                            nightSnr = foundSample.MeanTemporalSnr;
                        }

                        var filterNode = new TreeNodeViewModel(
                            filterKey.ToString(),
                            "Filter",
                            fileCount: filterGroup.Count(),
                            totalExposureMinutes: filterGroup.Sum(f => f.ExposureMinutes),
                            averageRms: filterGroup.Average(f => f.Rms),
                            averageHfr: filterGroup.Average(f => f.Hfr),
                            snr: nightSnr,
                            maxRms: filterGroup.Max(f => f.Rms),
                            maxHfr: filterGroup.Max(f => f.Hfr));

                        if (snrSample != null)
                        {
                            if (!projectFilterSnrSamples.TryGetValue(filterKey, out var samples))
                            {
                                samples = new List<SnrSample>();
                                projectFilterSnrSamples[filterKey] = samples;
                            }

                            samples.Add(snrSample);
                        }

                        nightNode.AddChild(filterNode);
                    }

                    nightNodes.Add(nightNode);
                }

                // Add project-level filter summary rows first.
                foreach (var filter in FilterDisplayOrder)
                {
                    var filterFiles = projectFiles.Where(f => f.Filter == filter).ToList();
                    if (filterFiles.Count == 0)
                    {
                        continue;
                    }

                    double? aggregateSnr = null;
                    var requiredSubordinateCount = projectFilterSubordinateCounts.TryGetValue(filter, out var count)
                        ? count
                        : 0;

                    var allNightsHaveSnr = totalNightCount > 0 && nightsWithSnrDataCount == totalNightCount;
                    if (allNightsHaveSnr &&
                        TryGetAggregateSnr(projectFilterSnrSamples, filter, requiredSubordinateCount, out var snr))
                    {
                        aggregateSnr = snr;
                    }
                    var filterSummaryNode = new TreeNodeViewModel(
                        filter.ToString(),
                        "Filter",
                        fileCount: filterFiles.Count,
                        totalExposureMinutes: filterFiles.Sum(f => f.ExposureMinutes),
                        averageRms: filterFiles.Any() ? filterFiles.Average(f => f.Rms) : null,
                        averageHfr: filterFiles.Any() ? filterFiles.Average(f => f.Hfr) : null,
                        snr: aggregateSnr,
                        maxRms: filterFiles.Any() ? filterFiles.Max(f => f.Rms) : null,
                        maxHfr: filterFiles.Any() ? filterFiles.Max(f => f.Hfr) : null);
                    projectNode.AddChild(filterSummaryNode);
                }

                foreach (var nightNode in nightNodes)
                {
                    projectNode.AddChild(nightNode);
                }

                telescopeNode.AddChild(projectNode);
            }

            TreeNodes.Add(telescopeNode);
        }

        RefreshVisibleTreeNodes();
    }

    private void ToggleNodeExpansion(TreeNodeViewModel? node)
    {
        if (node == null || !node.HasChildren)
        {
            return;
        }

        node.IsExpanded = !node.IsExpanded;
        RefreshVisibleTreeNodes();
    }

    private void RefreshVisibleTreeNodes()
    {
        VisibleTreeNodes.Clear();

        foreach (var rootNode in TreeNodes)
        {
            AddVisibleNodeRecursive(rootNode);
        }
    }

    private void AddVisibleNodeRecursive(TreeNodeViewModel node)
    {
        VisibleTreeNodes.Add(node);

        if (!node.IsExpanded)
        {
            return;
        }

        foreach (var child in node.Children)
        {
            AddVisibleNodeRecursive(child);
        }
    }

    private static int GetFilterSortOrder(char filter)
    {
        var index = Array.IndexOf(FilterDisplayOrder, filter);
        return index >= 0 ? index : 99;
    }

    private static Dictionary<string, Dictionary<char, SnrSample>> BuildNightFilterSnrMap(IEnumerable<AstronomyData> projectFiles)
    {
        var map = new Dictionary<string, Dictionary<char, SnrSample>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in projectFiles)
        {
            if (string.IsNullOrWhiteSpace(file.NightFolderPath) || map.ContainsKey(file.NightFolderPath))
            {
                continue;
            }

            map[file.NightFolderPath] = LoadNightFilterSnr(file.NightFolderPath);
        }

        return map;
    }

    private static Dictionary<char, SnrSample> LoadNightFilterSnr(string nightFolderPath)
    {
        var result = new Dictionary<char, SnrSample>();
        var lightFolderPath = Path.Combine(nightFolderPath, "LIGHT");
        var snrFilePath = Path.Combine(lightFolderPath, ApexAstroSnrFileName);
        if (!File.Exists(snrFilePath))
        {
            return result;
        }

        try
        {
            var json = File.ReadAllText(snrFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return result;
            }

            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            if (!TryGetPropertyCaseInsensitive(document.RootElement, "FilterSummaries", out var filtersElement)
                || filtersElement.ValueKind != JsonValueKind.Array)
            {
                if (!TryGetPropertyCaseInsensitive(document.RootElement, "filters", out filtersElement)
                    || filtersElement.ValueKind != JsonValueKind.Array)
                {
                    return result;
                }
            }

            foreach (var filterElement in filtersElement.EnumerateArray())
            {
                if (filterElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (!TryGetJsonString(filterElement, "filterName", out var filterName) ||
                    string.IsNullOrWhiteSpace(filterName) ||
                    !TryParseSnrFilter(filterName, out var filterKey) ||
                    !TryGetJsonDouble(filterElement, "meanTemporalSnr", out var meanTemporalSnr))
                {
                    continue;
                }

                var frameCount = TryGetJsonInt(filterElement, "frameCount", out var parsedFrameCount)
                    ? Math.Max(0, parsedFrameCount)
                    : 0;

                double? integrationMinutes = null;
                if (TryGetJsonDouble(filterElement, "integrationMinutes", out var parsedIntegrationMinutes))
                {
                    integrationMinutes = Math.Max(0, parsedIntegrationMinutes);
                }

                result[filterKey] = new SnrSample(meanTemporalSnr, frameCount, integrationMinutes);
            }
        }
        catch (IOException)
        {
            return new Dictionary<char, SnrSample>();
        }
        catch (UnauthorizedAccessException)
        {
            return new Dictionary<char, SnrSample>();
        }
        catch (JsonException)
        {
            return new Dictionary<char, SnrSample>();
        }

        return result;
    }

    private static bool TryGetNightFilterSnr(
        IReadOnlyDictionary<string, Dictionary<char, SnrSample>> nightFilterSnrByPath,
        string nightFolderPath,
        char filter,
        out SnrSample sample)
    {
        sample = null!;

        if (!nightFilterSnrByPath.TryGetValue(nightFolderPath, out var nightSnr))
        {
            return false;
        }

        if (!nightSnr.TryGetValue(filter, out var foundSample))
        {
            return false;
        }

        sample = foundSample;
        return true;
    }

    private static bool TryGetAggregateSnr(
        IReadOnlyDictionary<char, List<SnrSample>> projectFilterSnrSamples,
        char filter,
        int requiredSubordinateCount,
        out double aggregateSnr)
    {
        aggregateSnr = 0;

        if (requiredSubordinateCount <= 0)
        {
            return false;
        }

        if (!projectFilterSnrSamples.TryGetValue(filter, out var samples) || samples.Count == 0)
        {
            return false;
        }

        // If any subordinate filter row is missing SNR, the aggregate is considered unavailable.
        if (samples.Count < requiredSubordinateCount)
        {
            return false;
        }

        var validSnrValues = samples
            .Select(s => s.MeanTemporalSnr)
            .Where(snr => !double.IsNaN(snr) && !double.IsInfinity(snr) && snr >= 0)
            .ToList();

        if (validSnrValues.Count < requiredSubordinateCount)
        {
            return false;
        }

        var sumOfSquares = validSnrValues.Sum(snr => snr * snr);
        aggregateSnr = Math.Sqrt(sumOfSquares);
        return true;
    }

    private static bool TryGetJsonString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (!TryGetPropertyCaseInsensitive(element, propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return true;
    }

    private static bool TryGetJsonDouble(JsonElement element, string propertyName, out double value)
    {
        value = 0;
        if (!TryGetPropertyCaseInsensitive(element, propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number)
        {
            return property.TryGetDouble(out value);
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            var text = property.GetString();
            return double.TryParse(text, out value);
        }

        return false;
    }

    private static bool TryGetJsonInt(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        if (!TryGetPropertyCaseInsensitive(element, propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number)
        {
            return property.TryGetInt32(out value);
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            var text = property.GetString();
            return int.TryParse(text, out value);
        }

        return false;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement propertyValue)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                propertyValue = property.Value;
                return true;
            }
        }

        propertyValue = default;
        return false;
    }

    private static bool TryParseSnrFilter(string filterName, out char filter)
    {
        filter = default;
        var normalized = filterName.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (normalized.Length == 1 && normalized[0] is 'L' or 'R' or 'G' or 'B' or 'S' or 'H' or 'O')
        {
            filter = normalized[0];
            return true;
        }

        return normalized switch
        {
            "RED" => ReturnFilter('R', out filter),
            "GREEN" => ReturnFilter('G', out filter),
            "BLUE" => ReturnFilter('B', out filter),
            "LUMINANCE" => ReturnFilter('L', out filter),
            "LUM" => ReturnFilter('L', out filter),
            "SII" => ReturnFilter('S', out filter),
            "S2" => ReturnFilter('S', out filter),
            "HA" => ReturnFilter('H', out filter),
            "HALPHA" => ReturnFilter('H', out filter),
            "HYDROGEN" => ReturnFilter('H', out filter),
            "OIII" => ReturnFilter('O', out filter),
            "O3" => ReturnFilter('O', out filter),
            _ => false
        };
    }

    private static bool ReturnFilter(char value, out char filter)
    {
        filter = value;
        return true;
    }

    private sealed record OutOfDateSnrFolder(string LightDirectoryPath, string Reason);

    private sealed record SnrSample(double MeanTemporalSnr, int FrameCount, double? IntegrationMinutes);

    private static bool IsNightReviewed(string nightFolderPath)
    {
        foreach (var markerPath in GetReviewMarkerPathsInOrder(nightFolderPath))
        {
            if (TryGetReviewedValue(markerPath, out var reviewed))
            {
                return reviewed;
            }
        }

        return false;
    }

    private void ToggleNightReview(string? nightFolderPath)
    {
        if (string.IsNullOrWhiteSpace(nightFolderPath))
        {
            StatusMessage = "Night folder path is missing.";
            return;
        }

        var normalizedNightFolderPath = Path.GetFullPath(nightFolderPath);
        if (!Directory.Exists(normalizedNightFolderPath))
        {
            StatusMessage = $"Night folder not found: {normalizedNightFolderPath}";
            return;
        }

        var markerPath = ResolveReviewMarkerPath(normalizedNightFolderPath);
        var currentReviewedValue = TryGetReviewedValue(markerPath, out var reviewedValueFromFile)
            ? reviewedValueFromFile
            : false;
        var newReviewedValue = !currentReviewedValue;

        if (!TryWriteReviewedValue(markerPath, newReviewedValue, out var writeError))
        {
            StatusMessage = $"Could not update review status: {writeError}";
            MessageBox.Show(
                $"Could not update review status for:\n{normalizedNightFolderPath}\n\n{writeError}",
                "Toggle Review Status",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        BuildTreeStructure();
        StatusMessage = $"Review status updated: {(newReviewedValue ? "Reviewed" : "Not Reviewed")}";
    }

    private static string ResolveReviewMarkerPath(string nightFolderPath)
    {
        foreach (var markerPath in GetReviewMarkerPathsInOrder(nightFolderPath))
        {
            if (File.Exists(markerPath))
            {
                return markerPath;
            }
        }

        return Path.Combine(nightFolderPath, ApexAstroReviewedFileName);
    }

    private static IEnumerable<string> GetReviewMarkerPathsInOrder(string nightFolderPath)
    {
        var markerPaths = new List<string>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var candidateMarkerPaths = new[]
        {
            Path.Combine(nightFolderPath, ApexAstroReviewedFileName),
            Path.Combine(nightFolderPath, "LIGHT", ApexAstroReviewedFileName)
        };

        foreach (var candidatePath in candidateMarkerPaths)
        {
            if (seenPaths.Add(candidatePath))
            {
                markerPaths.Add(candidatePath);
            }
        }

        var parentPath = Path.GetDirectoryName(nightFolderPath);
        if (!string.IsNullOrWhiteSpace(parentPath))
        {
            var parentMarkerPath = Path.Combine(parentPath, ApexAstroReviewedFileName);
            if (seenPaths.Add(parentMarkerPath))
            {
                markerPaths.Add(parentMarkerPath);
            }
        }

        try
        {
            if (Directory.Exists(nightFolderPath))
            {
                foreach (var nestedMarkerPath in Directory.EnumerateFiles(
                             nightFolderPath,
                             ApexAstroReviewedFileName,
                             SearchOption.AllDirectories))
                {
                    if (seenPaths.Add(nestedMarkerPath))
                    {
                        markerPaths.Add(nestedMarkerPath);
                    }
                }
            }
        }
        catch (IOException)
        {
            // Ignore recursive scan failures and continue with direct candidates.
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore recursive scan failures and continue with direct candidates.
        }

        return markerPaths;
    }

    private static bool TryWriteReviewedValue(string markerPath, bool reviewed, out string error)
    {
        error = string.Empty;

        try
        {
            JsonObject root;

            if (File.Exists(markerPath))
            {
                var content = File.ReadAllText(markerPath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var parsedNode = JsonNode.Parse(content);
                    root = parsedNode as JsonObject ?? new JsonObject();
                }
                else
                {
                    root = new JsonObject();
                }
            }
            else
            {
                root = new JsonObject();
            }

            var reviewedPropertyName = root
                .Select(property => property.Key)
                .FirstOrDefault(name => name.Equals("Reviewed", StringComparison.OrdinalIgnoreCase))
                ?? "Reviewed";
            var reviewedUtcPropertyName = root
                .Select(property => property.Key)
                .FirstOrDefault(name => name.Equals("ReviewedUtc", StringComparison.OrdinalIgnoreCase))
                ?? "ReviewedUtc";

            root[reviewedPropertyName] = reviewed;
            root[reviewedUtcPropertyName] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

            var markerDirectoryPath = Path.GetDirectoryName(markerPath);
            if (!string.IsNullOrWhiteSpace(markerDirectoryPath))
            {
                Directory.CreateDirectory(markerDirectoryPath);
            }

            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(markerPath, json);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryGetReviewedValue(string markerPath, out bool reviewed)
    {
        reviewed = false;

        if (!File.Exists(markerPath))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(markerPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!property.Name.Equals("Reviewed", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.True)
                {
                    reviewed = true;
                    return true;
                }

                if (property.Value.ValueKind == JsonValueKind.False)
                {
                    reviewed = false;
                    return true;
                }

                if (property.Value.ValueKind == JsonValueKind.String
                    && bool.TryParse(property.Value.GetString(), out var parsed))
                {
                    reviewed = parsed;
                    return true;
                }

                return false;
            }

            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
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

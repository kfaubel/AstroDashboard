using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace AstroDashboard.ViewModels;

public class TreeNodeViewModel : BaseViewModel
{
    private bool _isExpanded;
    private bool _isSelected;
    private ObservableCollection<TreeNodeViewModel> _children;

    public string Name { get; }
    public string NodeType { get; } // "Telescope", "Project", "Night"
    public string? AssociatedData { get; }
    public bool? Reviewed { get; }
    public bool HasChildren => _children.Count > 0;
    public int Depth { get; private set; }
    public int? FileCount { get; }
    public double? TotalExposureMinutes { get; }
    public double? AverageRms { get; }
    public double? MaxRms { get; }
    public double? AverageHfr { get; }
    public double? MaxHfr { get; }
    public string FileCountText => FileCount.HasValue ? FileCount.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
    public string MinutesText => TotalExposureMinutes.HasValue
        ? FormatAsHoursMinutes(TotalExposureMinutes.Value)
        : string.Empty;
    public string AverageRmsText => FormatAvgMax(AverageRms, MaxRms);
    public string AverageHfrText => FormatAvgMax(AverageHfr, MaxHfr);
    public string ReviewStatusText => NodeType == "Night"
        ? (Reviewed == true ? "Reviewed" : "Not Reviewed")
        : string.Empty;

    private static string FormatAsHoursMinutes(double totalExposureMinutes)
    {
        var totalMinutes = Math.Max(0, (int)Math.Round(totalExposureMinutes, 0, MidpointRounding.AwayFromZero));
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}", hours, minutes);
    }

    private static string FormatAvgMax(double? average, double? max)
    {
        if (!average.HasValue || !max.HasValue)
        {
            return string.Empty;
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:F2}/{1:F2}",
            average.Value,
            max.Value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ObservableCollection<TreeNodeViewModel> Children
    {
        get => _children;
        set => SetProperty(ref _children, value);
    }

    public TreeNodeViewModel(
        string name,
        string nodeType,
        string? associatedData = null,
        bool? reviewed = null,
        int depth = 0,
        int? fileCount = null,
        double? totalExposureMinutes = null,
        double? averageRms = null,
        double? averageHfr = null,
        double? maxRms = null,
        double? maxHfr = null)
    {
        Name = name;
        NodeType = nodeType;
        AssociatedData = associatedData;
        Reviewed = reviewed;
        Depth = depth;
        FileCount = fileCount;
        TotalExposureMinutes = totalExposureMinutes;
        AverageRms = averageRms;
        MaxRms = maxRms;
        AverageHfr = averageHfr;
        MaxHfr = maxHfr;
        _children = new ObservableCollection<TreeNodeViewModel>();
        _isExpanded = false;
        _isSelected = false;
    }

    public void AddChild(TreeNodeViewModel child)
    {
        child.SetDepthRecursive(Depth + 1);
        Children.Add(child);
        OnPropertyChanged(nameof(HasChildren));
    }

    private void SetDepthRecursive(int depth)
    {
        Depth = depth;
        foreach (var nestedChild in Children)
        {
            nestedChild.SetDepthRecursive(depth + 1);
        }
    }
}

public class BaseViewModel : INotifyPropertyChanged
{
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

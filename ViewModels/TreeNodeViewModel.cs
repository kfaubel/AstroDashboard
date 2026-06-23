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
    public int Depth { get; private set; }
    public int? FileCount { get; }
    public double? TotalExposureMinutes { get; }
    public string FileCountText => FileCount.HasValue ? FileCount.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
    public string MinutesText => TotalExposureMinutes.HasValue
        ? Math.Round(TotalExposureMinutes.Value, 0, MidpointRounding.AwayFromZero).ToString("F0", CultureInfo.InvariantCulture)
        : string.Empty;

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
        int depth = 0,
        int? fileCount = null,
        double? totalExposureMinutes = null)
    {
        Name = name;
        NodeType = nodeType;
        AssociatedData = associatedData;
        Depth = depth;
        FileCount = fileCount;
        TotalExposureMinutes = totalExposureMinutes;
        _children = new ObservableCollection<TreeNodeViewModel>();
        _isExpanded = false;
        _isSelected = false;
    }

    public void AddChild(TreeNodeViewModel child)
    {
        child.Depth = Depth + 1;
        Children.Add(child);
    }
}

public class BaseViewModel : INotifyPropertyChanged
{
    protected void SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

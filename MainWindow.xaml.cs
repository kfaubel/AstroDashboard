using AstroDashboard.ViewModels;
using AstroDashboard.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AstroDashboard;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        var viewModel = new MainViewModel();
        this.DataContext = viewModel;
        
        // Check for initial path from command line, then saved session path, then current directory
        var initialPath = Application.Current.Properties["InitialPath"] as string;
        if (string.IsNullOrEmpty(initialPath))
        {
            initialPath = new PathStateService().GetLastPath() ?? Environment.CurrentDirectory;
        }
        
        viewModel.LoadInitialPath(initialPath);
    }
}

/// <summary>
/// Converts a boolean to FontWeight (Bold if true, Normal if false)
/// </summary>
public class BoolToFontWeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSubtotal && isSubtotal)
        {
            return FontWeight.FromOpenTypeWeight(700); // Bold
        }
        return FontWeight.FromOpenTypeWeight(400); // Normal
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean to Background color (Light gray for grand total)
/// </summary>
public class BoolToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isGrandTotal && isGrandTotal)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 250)); // Light blue
        }
        return System.Windows.Media.Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts node type to a display color
/// </summary>
public class NodeTypeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string nodeType)
        {
            return nodeType switch
            {
                "Telescope" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 102, 204)), // Blue
                "Project" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 153, 76)), // Green
                "Filter" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 92, 0)), // Orange
                "Night" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80)), // Gray
                _ => System.Windows.Media.Brushes.Black
            };
        }
        return System.Windows.Media.Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts node type to FontWeight, using bold for project rows.
/// </summary>
public class NodeTypeToFontWeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string nodeType && nodeType == "Project")
        {
            return FontWeight.FromOpenTypeWeight(700);
        }

        return FontWeight.FromOpenTypeWeight(400);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Applies TreeView depth offsets for row-level indent cancellation and name-cell indentation.
/// </summary>
public class DepthToNumericColumnMarginConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var depth = value is int d ? d : 0;
        // TreeView indents children by roughly one toggle-width per level.
        var indent = depth * 19;
        var mode = (parameter as string) ?? string.Empty;

        return mode switch
        {
            // Pull the whole row back left by depth so non-name columns remain globally aligned.
            "Row" => new Thickness(-indent, 1, 0, 1),
            // Reapply indent only to the name column so hierarchy remains visible.
            "Name" => new Thickness(indent, 2, 8, 2),
            _ => new Thickness(8, 2, 8, 2)
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a node type to Visibility when it matches the converter parameter.
/// </summary>
public class NodeTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var nodeType = value as string;
        var targetNodeType = parameter as string;

        if (!string.IsNullOrWhiteSpace(nodeType) &&
            !string.IsNullOrWhiteSpace(targetNodeType) &&
            string.Equals(nodeType, targetNodeType, StringComparison.OrdinalIgnoreCase))
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean to Visibility (Visible if true, Collapsed if false)
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isVisible && isVisible)
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

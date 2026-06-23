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
/// Offsets numeric columns to compensate for default TreeView indentation at deeper levels.
/// </summary>
public class DepthToNumericColumnMarginConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var depth = value is int d ? d : 0;
        var leftOffset = 8 - (depth * 36);
        return new Thickness(leftOffset, 2, 8, 2);
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

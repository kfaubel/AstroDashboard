using System.Windows;

namespace AstroDashboard.Views;

public partial class ThemedMessageDialog : Window
{
    public ThemedMessageDialog(string title, string message, string? details = null, string okButtonText = "OK", string? cancelButtonText = null)
    {
        InitializeComponent();

        Title = title;
        MessageTextBlock.Text = message;
        OkButtonControl.Content = okButtonText;

        if (string.IsNullOrWhiteSpace(details))
        {
            DetailsTextBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            DetailsTextBox.Text = details;
            DetailsTextBox.Visibility = Visibility.Visible;
        }

        if (string.IsNullOrWhiteSpace(cancelButtonText))
        {
            CancelButtonControl.Visibility = Visibility.Collapsed;
        }
        else
        {
            CancelButtonControl.Content = cancelButtonText;
            CancelButtonControl.Visibility = Visibility.Visible;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    public static bool ShowConfirmation(Window? owner, string title, string message, string? details = null, string okButtonText = "OK", string cancelButtonText = "Cancel")
    {
        var dialog = new ThemedMessageDialog(title, message, details, okButtonText, cancelButtonText)
        {
            Owner = owner
        };

        return dialog.ShowDialog() == true;
    }

    public static void ShowMessage(Window? owner, string title, string message, string? details = null, string okButtonText = "OK")
    {
        var dialog = new ThemedMessageDialog(title, message, details, okButtonText)
        {
            Owner = owner
        };

        dialog.ShowDialog();
    }
}
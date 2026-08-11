using System.ComponentModel;
using System.Windows;

namespace AstroDashboard.Views;

public partial class SnrProgressWindow : Window
{
    private bool _isCompleted;

    public event EventHandler? CancelRequested;

    public SnrProgressWindow()
    {
        InitializeComponent();
    }

    public void AppendLine(string line)
    {
        OutputTextBox.AppendText(line + Environment.NewLine);
        OutputTextBox.ScrollToEnd();
    }

    public void MarkCancelling()
    {
        if (_isCompleted)
        {
            return;
        }

        ActionButton.IsEnabled = false;
        ActionButton.Content = "Cancelling...";
    }

    public void MarkCompleted()
    {
        _isCompleted = true;
        ActionButton.IsEnabled = true;
        ActionButton.Content = "Close";
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isCompleted)
        {
            Close();
            return;
        }

        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isCompleted)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}

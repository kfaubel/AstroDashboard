using System.IO;
using System.Windows.Forms;

namespace AstroDashboard.Services;

public static class FolderDialog
{
    public static string? SelectFolder(string title, string? initialPath = null)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = title,
            ShowNewFolderButton = true,
            UseDescriptionForTitle = true
        };

        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
        {
            dialog.InitialDirectory = initialPath;
            dialog.SelectedPath = initialPath;
        }

        return dialog.ShowDialog() == DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }
}

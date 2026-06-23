using System.Windows;
using System.Collections;
using System.Collections.Generic;

namespace AstroDashboard;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Pass command-line argument to main window if provided
        if (e.Args.Length > 0)
        {
            this.Properties["InitialPath"] = e.Args[0];
        }
    }
}

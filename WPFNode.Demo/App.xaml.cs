using System.Windows;
using WPFNode.Demo.Models;
using WPFNode.Services;

namespace WPFNode.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        NodeServices.Initialize("Plugins");
        
        var mainWindow = new MainWindow();
        mainWindow.Show();
        
        
    }
} 
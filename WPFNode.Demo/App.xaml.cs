using System.Windows;
using WPFNode.Services;

namespace WPFNode.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        NodeServices.Initialize("Plugins");
    }
} 
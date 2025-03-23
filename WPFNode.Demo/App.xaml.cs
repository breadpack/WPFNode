using System.Windows;
using WPFNode.Demo.Models;
using WPFNode.Services;

namespace WPFNode.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e) {
        Application.Current.DispatcherUnhandledException += (sender, args) => {
            MessageBox.Show(args.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        
        base.OnStartup(e);
        NodeServices.Initialize("Plugins");
        
        var mainWindow = new MainWindow();
        mainWindow.Show();
        
        
    }
} 
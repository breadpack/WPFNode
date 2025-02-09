using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WPFNode.Core.DependencyInjection;
using WPFNode.Demo.ViewModels;

namespace WPFNode.Demo;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public IServiceProvider? ServiceProvider => _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddWPFNodeCore();
        services.AddTransient<MainWindowViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _serviceProvider?.Dispose();
    }
} 
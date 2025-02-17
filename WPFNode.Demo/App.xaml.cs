using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WPFNode.Demo.ViewModels;
using WPFNode.Plugins.Basic;
using WPFNode.Services;

namespace WPFNode.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 플러그인 초기화
        var pluginService = NodeServices.PluginService;

        // 기본 플러그인의 리소스를 Application.Resources에 추가
        // var basicPlugin = new BasicNodePlugin();
        // foreach (var resourceDictionary in basicPlugin.GetNodeStyles())
        // {
        //     Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
        // }
    }
} 
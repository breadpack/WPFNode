using WPFNode.Core.Interfaces;
using WPFNode.Core.Services;
using System.IO;
using System.Reflection;

namespace WPFNode.Core.Services;

public static class NodeServices
{
    private static readonly Lazy<INodePluginService> _pluginService = 
        new(() => {
            var nodePluginService = new NodePluginService();
            nodePluginService.LoadPlugins(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty);
            return nodePluginService;
        });
    
    private static readonly Lazy<INodeCommandService> _commandService = 
        new(() => new NodeCommandService(_pluginService.Value));

    public static INodePluginService PluginService => _pluginService.Value;
    public static INodeCommandService CommandService => _commandService.Value;

    public static void Initialize(string pluginPath)
    {
        // 외부 플러그인 로드
        if (!string.IsNullOrEmpty(pluginPath) && Directory.Exists(pluginPath))
        {
            PluginService.LoadPlugins(pluginPath);
        }
    }
} 
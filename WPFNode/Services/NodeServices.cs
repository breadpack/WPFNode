using System.IO;
using System.Reflection;
using WPFNode.Interfaces;

namespace WPFNode.Services;

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

    private static readonly PropertyControlProviderRegistry _propertyControlProviderRegistry = new();

    static NodeServices()
    {
        _propertyControlProviderRegistry.RegisterProviders();
    }

    public static INodePluginService PluginService => _pluginService.Value;
    public static INodeCommandService CommandService => _commandService.Value;
    public static PropertyControlProviderRegistry PropertyControlProviderRegistry => _propertyControlProviderRegistry;

    public static void Initialize(string pluginPath)
    {
        // 외부 플러그인 로드
        if (!string.IsNullOrEmpty(pluginPath) && Directory.Exists(pluginPath))
        {
            PluginService.LoadPlugins(pluginPath);
        }
    }
} 
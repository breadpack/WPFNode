using Microsoft.Extensions.DependencyInjection;
using WPFNode.Core.Services;
using WPFNode.Core.Interfaces;
using System.Reflection;
using WPFNode.Abstractions;

namespace WPFNode.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWPFNodeCore(this IServiceCollection services)
    {
        // 기본 서비스 등록
        services.AddSingleton<INodePluginService>(sp =>
        {
            var pluginService = new NodePluginService();
            
            // 기본 플러그인 타입들을 직접 로드
            var basicPluginAssembly = Assembly.Load("WPFNode.Plugins.Basic");
            var nodeTypes = basicPluginAssembly.GetTypes()
                .Where(t => typeof(INode).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var nodeType in nodeTypes)
            {
                pluginService.RegisterNodeType(nodeType);
            }
            
            return pluginService;
        });
        
        services.AddSingleton<INodeCommandService, NodeCommandService>();
        
        return services;
    }
} 
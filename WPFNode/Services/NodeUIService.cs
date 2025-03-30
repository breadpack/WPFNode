using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Services;

public class NodeUIService : INodeUIService
{
    private readonly ConcurrentDictionary<string, ResourceDictionary> _resourceCache = new();
    private readonly List<ResourceDictionary> _pluginResourceDictionaries = new();
    private readonly ILogger<NodeUIService>? _logger;
    private readonly INodeModelService _modelService;
    
    static NodeUIService()
    {
        // 빌트인 PropertyControlProvider 등록
        PropertyControlProviderRegistry.RegisterProviders();
    }
    
    public NodeUIService(INodeModelService modelService, ILogger<NodeUIService>? logger = null)
    {
        _modelService = modelService;
        _logger = logger;
        
        // UI 플러그인 로드
        LoadUIPlugins();
    }
    
    private void LoadUIPlugins()
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && 
                           !a.GlobalAssemblyCache &&
                           a.GetName().Name?.StartsWith("WPFNode") == true)
                .ToList();

            foreach (var assembly in assemblies)
            {
                LoadUIPluginsFromAssembly(assembly);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UI 플러그인 로드 중 오류 발생");
        }
    }

    private void LoadUIPluginsFromAssembly(Assembly assembly)
    {
        try
        {
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IUINodePlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToList();
                
            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = Activator.CreateInstance(pluginType) as IUINodePlugin;
                    if (plugin != null)
                    {
                        // 스타일 로드
                        foreach (var resourceDictionary in plugin.GetNodeStyles())
                        {
                            _pluginResourceDictionaries.Add(resourceDictionary);
                        }
                        
                        // 속성 컨트롤 로드
                        foreach (var provider in plugin.GetPropertyControlProviders())
                        {
                            PropertyControlProviderRegistry.RegisterProvider(provider);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "UI 플러그인 인스턴스 생성 실패: {Type}", pluginType.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "어셈블리에서 UI 플러그인 로드 중 오류 발생: {Assembly}", assembly.FullName);
        }
    }
    
    public void LoadExternalUIPlugins(string pluginPath)
    {
        if (!Directory.Exists(pluginPath))
        {
            _logger?.LogWarning("플러그인 디렉토리를 찾을 수 없습니다: {Path}", pluginPath);
            return;
        }

        foreach (var dllFile in Directory.GetFiles(pluginPath, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                LoadUIPluginsFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "외부 UI 플러그인 어셈블리 로드 실패: {Path}", dllFile);
            }
        }
    }

    public Style? FindNodeStyle(Type nodeType)
    {
        if (nodeType == null) return null;

        try
        {
            var styleAttribute = nodeType.GetCustomAttribute<NodeStyleAttribute>();
            if (styleAttribute == null) return null;

            var resourceKey = styleAttribute.StyleResourceKey;
            
            // 1. Application 리소스에서 검색
            var style = Application.Current.TryFindResource(resourceKey) as Style;
            if (style != null) return style;
            
            // 2. 플러그인 리소스에서 검색
            foreach (var dictionary in _pluginResourceDictionaries)
            {
                if (dictionary.Contains(resourceKey) && dictionary[resourceKey] is Style pluginStyle)
                {
                    return pluginStyle;
                }
            }

            // 3. 캐시된 리소스에서 검색
            var assembly = nodeType.Assembly;
            var assemblyName = assembly.GetName().Name;
            var resourceFile = styleAttribute.ResourceFile;
            var cacheKey = $"{assemblyName}|{resourceFile}";

            return _resourceCache.GetOrAdd(cacheKey, key =>
            {
                try
                {
                    var uri = new Uri($"pack://application:,,,/{assemblyName};component/{resourceFile}", UriKind.Absolute);
                    return new ResourceDictionary { Source = uri };
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, LoggerMessages.ResourceDictionaryLoadFailed, assemblyName, resourceFile);
                    return null;
                }
            })?[resourceKey] as Style;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "노드 스타일 검색 중 오류 발생: {Type}", nodeType.FullName);
            return null;
        }
    }
} 
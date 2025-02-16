using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Logging;
using WPFNode.Abstractions;
using WPFNode.Core.Interfaces;
using WPFNode.Core.Models;
using System.Windows;
using System.Collections.Concurrent;
using WPFNode.Abstractions.Attributes;

namespace WPFNode.Core.Services;

public class NodePluginService : INodePluginService, IDisposable
{
    private readonly ConcurrentDictionary<Type, NodeMetadata> _nodeTypes = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _categoryCache = new();
    private readonly ConcurrentDictionary<string, ResourceDictionary> _resourceCache = new();
    private readonly ILogger<NodePluginService>? _logger;
    private bool _isDisposed;
    
    public NodePluginService(ILogger<NodePluginService>? logger = null)
    {
        _logger = logger;
        LoadAllAssemblies();
    }

    private void LoadAllAssemblies()
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(IsValidAssembly)
                .ToList();

            foreach (var assembly in assemblies)
            {
                LoadNodesFromAssembly(assembly);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "어셈블리 로드 중 오류 발생");
        }
    }

    private bool IsValidAssembly(Assembly assembly)
    {
        if (assembly == null) return false;
        
        // 시스템 어셈블리 제외
        if (assembly.IsDynamic || 
            assembly.GlobalAssemblyCache ||
            assembly.FullName?.StartsWith("System.") == true ||
            assembly.FullName?.StartsWith("Microsoft.") == true)
            return false;

        // WPFNode 관련 어셈블리만 포함
        var assemblyName = assembly.GetName().Name;
        return assemblyName?.StartsWith("WPFNode") == true;
    }

    private void LoadNodesFromAssembly(Assembly assembly)
    {
        try
        {
            _logger?.LogInformation("어셈블리 검사 중: {Assembly}", assembly.FullName);

            var nodeTypes = assembly.GetTypes()
                .Where(IsValidNodeType)
                .ToList();

            foreach (var nodeType in nodeTypes)
            {
                try
                {
                    RegisterNodeType(nodeType);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "노드 타입 등록 실패: {Type}", nodeType.FullName);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger?.LogError(ex, "어셈블리 타입 로드 실패: {Assembly}", assembly.FullName);
            LogLoaderExceptions(ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "어셈블리 처리 중 오류 발생: {Assembly}", assembly.FullName);
        }
    }

    private void LogLoaderExceptions(ReflectionTypeLoadException ex)
    {
        foreach (var loaderException in ex.LoaderExceptions)
        {
            if (loaderException != null)
            {
                _logger?.LogError(loaderException, "로더 예외");
            }
        }
    }

    private bool IsValidNodeType(Type type)
    {
        if (type == null) return false;
        
        var hasValidConstructor = type.GetConstructor(Type.EmptyTypes) != null ||
                                type.GetConstructor(new[] { typeof(INodeCanvas) }) != null;
        
        return typeof(INode).IsAssignableFrom(type) && 
               !type.IsInterface && 
               !type.IsAbstract &&
               hasValidConstructor;
    }

    public void LoadPlugins(string pluginPath)
    {
        if (!Directory.Exists(pluginPath))
        {
            _logger?.LogWarning("플러그인 디렉토리가 존재하지 않습니다: {Path}", pluginPath);
            return;
        }

        foreach (var dllFile in Directory.GetFiles(pluginPath, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                if (IsValidAssembly(assembly))
                {
                    LoadNodesFromAssembly(assembly);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "외부 플러그인 어셈블리 로드 실패: {Path}", dllFile);
            }
        }
    }

    public IReadOnlyCollection<Type> NodeTypes => _nodeTypes.Keys.ToList();

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

            // 2. 캐시된 리소스에서 검색
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
                    _logger?.LogWarning(ex, "리소스 딕셔너리 로드 실패: {Assembly}, {ResourceFile}", assemblyName, resourceFile);
                    return null;
                }
            })?[resourceKey] as Style;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "노드 스타일 검색 중 오류 발생: {NodeType}", nodeType.Name);
            return null;
        }
    }

    public void RegisterNodeType(Type nodeType)
    {
        if (nodeType == null)
            throw new ArgumentNullException(nameof(nodeType));

        if (!IsValidNodeType(nodeType))
            throw new ArgumentException($"유효하지 않은 노드 타입입니다: {nodeType.Name}");

        _nodeTypes.TryAdd(nodeType, CreateNodeMetadata(nodeType));
        
        // 카테고리 캐시 업데이트
        var metadata = GetNodeMetadata(nodeType);
        _categoryCache.AddOrUpdate(
            metadata.Category,
            new HashSet<string> { nodeType.FullName ?? nodeType.Name },
            (_, types) =>
            {
                types.Add(nodeType.FullName ?? nodeType.Name);
                return types;
            });
    }

    public INode CreateNode(Type nodeType)
    {
        if (!_nodeTypes.ContainsKey(nodeType))
        {
            throw new ArgumentException($"등록되지 않은 노드 타입입니다: {nodeType.Name}");
        }

        try
        {
            var node = (INode)Activator.CreateInstance(nodeType)!;
            node.Initialize();
            return node;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "노드 인스턴스 생성 실패: {Type}", nodeType.Name);
            throw;
        }
    }

    public IEnumerable<Type> GetNodeTypesByCategory(string category)
    {
        return _nodeTypes
            .Where(kvp => kvp.Value.Category == category)
            .Select(kvp => kvp.Key);
    }

    public IEnumerable<string> GetCategories()
    {
        return _categoryCache.Keys;
    }

    public NodeMetadata GetNodeMetadata(Type nodeType)
    {
        return _nodeTypes.GetOrAdd(nodeType, CreateNodeMetadata);
    }

    private static NodeMetadata CreateNodeMetadata(Type nodeType)
    {
        if (nodeType == null)
            throw new ArgumentNullException(nameof(nodeType));

        if (!typeof(INode).IsAssignableFrom(nodeType))
            throw new ArgumentException($"타입이 INode를 구현하지 않습니다: {nodeType.Name}");

        var nameAttr = nodeType.GetCustomAttribute<NodeNameAttribute>();
        var categoryAttr = nodeType.GetCustomAttribute<NodeCategoryAttribute>();
        var descriptionAttr = nodeType.GetCustomAttribute<NodeDescriptionAttribute>();
        var isOutputNode = nodeType.GetCustomAttribute<OutputNodeAttribute>() != null;
        
        return new NodeMetadata(
            nodeType,
            nameAttr?.Name ?? nodeType.Name,
            categoryAttr?.Category ?? "Basic",
            descriptionAttr?.Description ?? string.Empty,
            isOutputNode);
    }

    public IEnumerable<NodeMetadata> GetNodeMetadataByCategory(string category)
    {
        return _nodeTypes
            .Where(kvp => kvp.Value.Category == category)
            .Select(kvp => kvp.Value);
    }

    public IEnumerable<NodeMetadata> GetAllNodeMetadata()
    {
        return _nodeTypes.Values;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        foreach (var resourceDict in _resourceCache.Values)
        {
            resourceDict?.Clear();
        }
        _resourceCache.Clear();
        _nodeTypes.Clear();
        _categoryCache.Clear();
        
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
} 
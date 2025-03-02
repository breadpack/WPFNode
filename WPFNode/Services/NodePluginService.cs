using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Exceptions;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Services;

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
            _logger?.LogError(ex, LoggerMessages.AssemblyLoadError, "Core Assemblies");
            throw new NodePluginException("코어 어셈블리 로드 중 오류가 발생했습니다.", ex);
        }
    }

    private bool IsValidAssembly(Assembly assembly)
    {
        if (assembly == null) return false;
        
        try
        {
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
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "어셈블리 유효성 검사 중 오류 발생: {Assembly}", assembly.FullName);
            return false;
        }
    }

    private void LoadNodesFromAssembly(Assembly assembly)
    {
        try
        {
            _logger?.LogInformation(LoggerMessages.AssemblyInspecting, assembly.FullName);

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
                    _logger?.LogError(ex, LoggerMessages.NodeTypeRegistrationFailed, nodeType.FullName);
                    throw new NodePluginException(
                        $"노드 타입 등록 실패: {nodeType.FullName}", 
                        assembly.Location, 
                        ex);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger?.LogError(ex, LoggerMessages.AssemblyTypeLoadFailed, assembly.FullName);
            LogLoaderExceptions(ex);
            throw new NodePluginException(
                $"어셈블리 타입 로드 실패: {assembly.FullName}", 
                assembly.Location, 
                ex);
        }
        catch (Exception ex) when (ex is not NodePluginException)
        {
            _logger?.LogError(ex, LoggerMessages.AssemblyProcessingError, assembly.FullName);
            throw new NodePluginException(
                $"어셈블리 처리 중 오류 발생: {assembly.FullName}", 
                assembly.Location, 
                ex);
        }
    }

    private void LogLoaderExceptions(ReflectionTypeLoadException ex)
    {
        foreach (var loaderException in ex.LoaderExceptions)
        {
            if (loaderException != null)
            {
                _logger?.LogError(loaderException, "로더 예외: {Message}", loaderException.Message);
            }
        }
    }

    private bool IsValidNodeType(Type type)
    {
        if (type == null) return false;
        
        try
        {
            var hasValidConstructor = type.GetConstructor(Type.EmptyTypes) != null ||
                                    type.GetConstructor(new[] { typeof(INodeCanvas), typeof(Guid) }) != null;
            
            return typeof(INode).IsAssignableFrom(type) && 
                   !type.IsInterface && 
                   !type.IsAbstract &&
                   hasValidConstructor;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "타입 유효성 검사 중 오류 발생: {Type}", type.FullName);
            return false;
        }
    }

    public void LoadPlugins(string pluginPath)
    {
        if (!Directory.Exists(pluginPath))
        {
            _logger?.LogWarning(LoggerMessages.PluginDirectoryNotFound, pluginPath);
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
                _logger?.LogError(ex, LoggerMessages.ExternalPluginLoadFailed, dllFile);
                throw new NodePluginException(
                    $"외부 플러그인 어셈블리 로드 실패: {dllFile}", 
                    pluginPath, 
                    ex);
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
                    _logger?.LogWarning(ex, LoggerMessages.ResourceDictionaryLoadFailed, assemblyName, resourceFile);
                    return null;
                }
            })?[resourceKey] as Style;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, LoggerMessages.NodeStyleSearchError, nodeType.Name);
            return null;
        }
    }

    public void RegisterNodeType(Type nodeType)
    {
        if (nodeType == null)
            throw new ArgumentNullException(nameof(nodeType));

        if (!IsValidNodeType(nodeType))
            throw new NodeValidationException(
                string.Format(LoggerMessages.InvalidNodeType, nodeType.Name));

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
            throw new NodeValidationException(
                string.Format(LoggerMessages.UnregisteredNodeType, nodeType.Name));
        }

        try
        {
            var node = (INode)Activator.CreateInstance(nodeType)!;
            return node;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, LoggerMessages.NodeInstanceCreationFailed, nodeType.Name);
            throw new NodeValidationException(
                $"노드 인스턴스 생성 실패: {nodeType.Name}", ex);
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
            throw new NodeValidationException(
                $"타입이 INode를 구현하지 않습니다: {nodeType.Name}");

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
        
        _nodeTypes.Clear();
        _categoryCache.Clear();
        _resourceCache.Clear();
        
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
} 
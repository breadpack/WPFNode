using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Logging;
using WPFNode.Abstractions;
using WPFNode.Core.Interfaces;
using WPFNode.Core.Models;
using WPFNode.Core.Attributes;
using System.Windows;

namespace WPFNode.Core.Services;

public class NodePluginService : INodePluginService
{
    private readonly Dictionary<Type, NodeMetadata> _nodeTypes = new();
    private readonly ILogger<NodePluginService>? _logger;
    
    public NodePluginService(ILogger<NodePluginService>? logger = null)
    {
        _logger = logger;
        RegisterDefaultNodes();
    }

    public IReadOnlyCollection<Type> NodeTypes => _nodeTypes.Keys;

    public Style? FindNodeStyle(Type nodeType)
    {
        try
        {
            var styleAttribute = nodeType.GetCustomAttribute<NodeStyleAttribute>();
            if (styleAttribute == null) return null;

            var resourceKey = styleAttribute.StyleResourceKey;
            _logger?.LogDebug("스타일 검색 시작 - 노드: {NodeType}, 리소스키: {ResourceKey}", nodeType.Name, resourceKey);
            
            // 1. Application 리소스에서 검색
            var style = Application.Current.TryFindResource(resourceKey) as Style;
            if (style != null)
            {
                _logger?.LogDebug("스타일을 Application 리소스에서 찾음: {ResourceKey}", resourceKey);
                _logger?.LogDebug("Application 리소스 개수: {Count}", Application.Current.Resources.MergedDictionaries.Count);
                return style;
            }

            // 2. 플러그인 어셈블리의 리소스에서 검색
            var assembly = nodeType.Assembly;
            var assemblyName = assembly.GetName().Name;
            try
            {
                var resourceFile = styleAttribute.ResourceFile;
                var uri = new Uri($"pack://application:,,,/{assemblyName};component/{resourceFile}", UriKind.Absolute);
                _logger?.LogDebug("리소스 딕셔너리 로드 시도: {Uri}", uri);
                
                var resourceDictionary = new ResourceDictionary { Source = uri };
                _logger?.LogDebug("리소스 딕셔너리 로드 완료 - 리소스 개수: {Count}", resourceDictionary.Count);
                
                foreach (var key in resourceDictionary.Keys)
                {
                    _logger?.LogDebug("로드된 리소스 키: {Key}", key);
                }
                
                if (resourceDictionary.Contains(resourceKey))
                {
                    _logger?.LogDebug("스타일을 플러그인 리소스에서 찾음: {ResourceKey} in {ResourceFile}", resourceKey, resourceFile);
                    var foundStyle = resourceDictionary[resourceKey] as Style;
                    _logger?.LogDebug("찾은 스타일 정보 - Type: {StyleType}, BasedOn: {BasedOn}", 
                        foundStyle?.TargetType.Name, 
                        foundStyle?.BasedOn?.TargetType.Name);
                    return foundStyle;
                }
                else
                {
                    _logger?.LogWarning("리소스 딕셔너리에서 스타일을 찾을 수 없음: {ResourceKey} in {ResourceFile}", resourceKey, resourceFile);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "플러그인 리소스 로드 실패: {Assembly}, {Uri}", assemblyName, $"pack://application:,,,/{assemblyName};component/{styleAttribute.ResourceFile}");
            }

            _logger?.LogWarning("스타일을 찾을 수 없음: {ResourceKey}", resourceKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "노드 스타일 검색 중 오류 발생: {NodeType}", nodeType.Name);
            return null;
        }
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
                var nodeTypes = assembly.GetTypes()
                    .Where(t => typeof(INode).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var nodeType in nodeTypes)
                {
                    try
                    {
                        // 노드 타입이 매개변수 없는 생성자를 가지고 있는지 확인
                        var constructor = nodeType.GetConstructor(Type.EmptyTypes);
                        if (constructor == null)
                        {
                            _logger?.LogWarning("매개변수 없는 생성자가 없습니다: {Type}", nodeType.Name);
                            continue;
                        }

                        RegisterNodeType(nodeType);
                        _logger?.LogInformation("노드 타입 등록 성공: {Type}", nodeType.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "노드 타입 등록 실패: {Type}", nodeType.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "플러그인 어셈블리 로드 실패: {Path}", dllFile);
            }
        }
    }

    public void RegisterNodeType(Type nodeType)
    {
        if (nodeType == null)
            throw new ArgumentNullException(nameof(nodeType));

        if (!typeof(INode).IsAssignableFrom(nodeType))
            throw new ArgumentException($"타입이 INode를 구현하지 않습니다: {nodeType.Name}");

        if (nodeType.IsInterface || nodeType.IsAbstract)
            throw new ArgumentException($"인터페이스나 추상 클래스는 등록할 수 없습니다: {nodeType.Name}");

        var constructor = nodeType.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
            throw new ArgumentException($"매개변수 없는 생성자가 필요합니다: {nodeType.Name}");

        if (_nodeTypes.ContainsKey(nodeType))
        {
            _logger?.LogWarning("이미 등록된 노드 타입입니다: {Type}", nodeType.Name);
            return;
        }

        try
        {
            var metadata = CreateNodeMetadata(nodeType);
            _nodeTypes.Add(nodeType, metadata);
            _logger?.LogInformation("노드 타입 등록 성공: {Type}", nodeType.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "노드 타입 등록 실패: {Type}", nodeType.Name);
            throw;
        }
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
        return _nodeTypes.Values
            .Select(info => info.Category)
            .Distinct();
    }

    private void RegisterDefaultNodes()
    {
        // 기본 노드들 등록
        var defaultNodeTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => typeof(INode).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var nodeType in defaultNodeTypes)
        {
            try
            {
                RegisterNodeType(nodeType);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "기본 노드 타입 등록 실패: {Type}", nodeType.Name);
            }
        }
    }

    public NodeMetadata GetNodeMetadata(Type nodeType)
    {
        if (!_nodeTypes.TryGetValue(nodeType, out var metadata))
        {
            metadata = CreateNodeMetadata(nodeType);
            _nodeTypes[nodeType] = metadata;
        }

        return metadata;
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
} 
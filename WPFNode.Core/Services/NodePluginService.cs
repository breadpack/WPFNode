using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Logging;
using WPFNode.Abstractions;
using WPFNode.Core.Interfaces;
using WPFNode.Plugin.SDK;

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
        if (!typeof(INode).IsAssignableFrom(nodeType))
        {
            throw new ArgumentException($"타입이 INode를 구현하지 않습니다: {nodeType.Name}");
        }

        if (nodeType.IsInterface || nodeType.IsAbstract)
        {
            throw new ArgumentException($"인터페이스나 추상 클래스는 등록할 수 없습니다: {nodeType.Name}");
        }

        var constructor = nodeType.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
        {
            throw new ArgumentException($"매개변수 없는 생성자가 필요합니다: {nodeType.Name}");
        }

        if (_nodeTypes.ContainsKey(nodeType))
        {
            _logger?.LogWarning("이미 등록된 노드 타입입니다: {Type}", nodeType.Name);
            return;
        }

        try
        {
            _nodeTypes[nodeType] = Plugin.SDK.NodeBase.GetNodeMetadata(nodeType);
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
            throw new ArgumentException($"등록되지 않은 노드 타입입니다: {nodeType.Name}");
        }

        return metadata;
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
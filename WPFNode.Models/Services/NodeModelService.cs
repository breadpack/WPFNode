using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Exceptions;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Services;

/// <summary>
/// 노드 모델 서비스 구현
/// 노드 타입 관리 및 노드 생성
/// </summary>
public class NodeModelService : INodeModelService
{
    private readonly ConcurrentDictionary<Type, NodeMetadata> _nodeTypes = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _categoryCache = new();
    private readonly ILogger<NodeModelService>? _logger;
    private bool _isDisposed;
    
    public NodeModelService(ILogger<NodeModelService>? logger = null)
    {
        _logger = logger;
        LoadAllAssemblies();
    }

    private void LoadAllAssemblies()
    {
        try
        {
            Assembly.Load("WPFNode.Plugins.Basic");
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(IsValidAssembly)
                .ToList();

            _logger?.LogInformation("도메인에서 {Count}개의 어셈블리 발견", assemblies.Count);

            foreach (var assembly in assemblies)
            {
                try
                {
                    LoadNodesFromAssembly(assembly);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "어셈블리 {Assembly} 처리 중 오류 발생", assembly.FullName);
                    // 계속 진행
                }
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

            // 외부 플러그인 DLL도 허용
            return true;
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

            if (nodeTypes.Count > 0)
            {
                _logger?.LogInformation("어셈블리 {Assembly}에서 {Count}개의 노드 타입 발견", 
                    assembly.GetName().Name, nodeTypes.Count);
            }

            foreach (var nodeType in nodeTypes)
            {
                try
                {
                    RegisterNodeType(nodeType);
                    _logger?.LogInformation("노드 타입 등록: {NodeType}", nodeType.FullName);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, LoggerMessages.NodeTypeRegistrationFailed, nodeType.FullName);
                    // 예외 던지지 않고 계속 진행
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger?.LogError(ex, LoggerMessages.AssemblyTypeLoadFailed, assembly.FullName);
            LogLoaderExceptions(ex);
            // 예외 던지지 않고 계속 진행
        }
        catch (Exception ex) when (ex is not NodePluginException)
        {
            _logger?.LogError(ex, LoggerMessages.AssemblyProcessingError, assembly.FullName);
            // 예외 던지지 않고 계속 진행
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
            // 생성자 검증 조건 개선
            bool hasValidConstructor = HasValidConstructor(type);
            
            return typeof(INode).IsAssignableFrom(type) && 
                   type is { IsInterface: false, IsAbstract: false } &&
                   hasValidConstructor;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "타입 유효성 검사 중 오류 발생: {Type}", type.FullName);
            return false;
        }
    }

    // 생성자 검증 개선 메서드 추가
    private bool HasValidConstructor(Type type)
    {
        try
        {
            // 기본 생성자 또는 (INodeCanvas, Guid) 생성자 확인
            var hasDefaultCtor = type.GetConstructor(Type.EmptyTypes) != null;
            var hasCanvasGuidCtor = type.GetConstructor(new[] { typeof(INodeCanvas), typeof(Guid) }) != null;
            
            // 다른 가능한 생성자 패턴 확인
            if (hasDefaultCtor || hasCanvasGuidCtor)
                return true;
                
            // 모든 생성자 검사
            var constructors = type.GetConstructors();
            foreach (var ctor in constructors)
            {
                // 다른 유효한 생성자 패턴이 있는지 확인
                var parameters = ctor.GetParameters();
                if (parameters.Length <= 2)  // 매개변수가 2개 이하인 생성자도 허용
                    return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "생성자 유효성 검사 중 오류 발생: {Type}", type.FullName);
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

        var failedPlugins = new List<string>();

        foreach (var dllFile in Directory.GetFiles(pluginPath, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                _logger?.LogInformation("플러그인 로드 시도: {PluginPath}", dllFile);
                
                // 플러그인 로드 방식 개선: IsValidAssembly 검사 제거
                LoadNodesFromAssembly(assembly);
                
                // LoadNodePlugins 호출 제거 (중복 작업 방지)
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, LoggerMessages.ExternalPluginLoadFailed, dllFile);
                failedPlugins.Add(Path.GetFileName(dllFile));
                // 예외 발생 시 계속 진행
            }
        }

        if (failedPlugins.Count > 0)
        {
            _logger?.LogWarning("다음 플러그인 로드 실패 ({Count}개): {FailedPlugins}", 
                failedPlugins.Count, 
                string.Join(", ", failedPlugins));
        }
    }
    
    private void LoadNodePlugins(Assembly assembly)
    {
        try
        {
            // INodePlugin을 사용하지 않고 직접 INode 구현체를 찾아 등록
            var nodeTypes = assembly.GetTypes()
                .Where(t => typeof(INode).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
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
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "어셈블리에서 노드 타입 로드 중 오류 발생: {Assembly}", assembly.FullName);
        }
    }

    public IReadOnlyCollection<Type> NodeTypes => _nodeTypes.Keys.ToList();

    public void RegisterNodeType(Type nodeType)
    {
        if (!typeof(INode).IsAssignableFrom(nodeType))
            throw new ArgumentException("타입이 INode를 구현해야 합니다.", nameof(nodeType));
            
        if (_nodeTypes.ContainsKey(nodeType))
            return;
            
        var metadata = CreateNodeMetadata(nodeType);
        _nodeTypes[nodeType] = metadata;
    }

    public INode CreateNode(Type nodeType)
    {
        if (!typeof(INode).IsAssignableFrom(nodeType))
            throw new ArgumentException("타입이 INode를 구현해야 합니다.", nameof(nodeType));
            
        return (INode)Activator.CreateInstance(nodeType)!;
    }

    public IEnumerable<Type> GetNodeTypesByCategory(string category)
    {
        return _nodeTypes
            .Where(kvp => kvp.Value.Category == category)
            .Select(kvp => kvp.Key);
    }

    public IEnumerable<string> GetCategories()
    {
        return _nodeTypes.Values.Select(m => m.Category).Distinct().OrderBy(c => c);
    }

    public NodeMetadata GetNodeMetadata(Type nodeType)
    {
        if (_nodeTypes.TryGetValue(nodeType, out var metadata))
            return metadata;
            
        throw new ArgumentException($"노드 타입 {nodeType.Name}에 대한 메타데이터를 찾을 수 없습니다.");
    }

    private static NodeMetadata CreateNodeMetadata(Type nodeType)
    {
        var categoryAttr = nodeType.GetCustomAttribute<NodeCategoryAttribute>();
        var nameAttr = nodeType.GetCustomAttribute<NodeNameAttribute>();
        var descAttr = nodeType.GetCustomAttribute<NodeDescriptionAttribute>();
        
        return new NodeMetadata
        {
            NodeType    = nodeType,
            Category    = categoryAttr?.Category ?? "기타",
            Name        = nameAttr?.Name ?? nodeType.Name,
            Description = descAttr?.Description ?? string.Empty
        };
    }

    public IEnumerable<NodeMetadata> GetNodeMetadataByCategory(string category)
    {
        return _nodeTypes.Values.Where(m => m.Category == category).OrderBy(m => m.Name);
    }

    public IEnumerable<NodeMetadata> GetAllNodeMetadata()
    {
        return _nodeTypes.Values.OrderBy(m => m.Category).ThenBy(m => m.Name);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        // 필요한 정리 작업 수행
        _nodeTypes.Clear();
        _categoryCache.Clear();
        
        _isDisposed = true;
    }
} 
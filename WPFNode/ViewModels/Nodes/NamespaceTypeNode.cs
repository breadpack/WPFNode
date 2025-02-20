using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using WPFNode.Services;
using WPFNode.Interfaces;
using WPFNode.Utilities;
using System.Windows;
using System.Windows.Threading;

namespace WPFNode.ViewModels.Nodes;

public class NamespaceTypeNode : ObservableObject
{
    private static readonly Dictionary<string, (List<Type> Types, List<string> ChildNamespaces)> _cache = new();
    private readonly bool _isPluginOnly;
    private ObservableCollection<object>? _children;
    private ObservableCollection<Type>? _types;
    private readonly List<NamespaceTypeNode> _childNamespaces = new();
    private bool _isExpanded;
    private bool _isLoaded;
    private IEnumerable<Type>? _filteredTypes;  // 검색 시 필터링된 타입들
    private bool _isUpdating;
    private bool? _hasMatchedTypesCache;
    private bool _isFiltering;

    public string Name { get; }
    public string FullNamespace { get; }
    public bool IsNamespace { get; }

    public ObservableCollection<Type> Types
    {
        get => _types ??= new ObservableCollection<Type>();
        private set => SetProperty(ref _types, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value) && value && !_isLoaded)
            {
                LoadChildren();
            }
        }
    }

    public ObservableCollection<object> Children 
    { 
        get
        {
            if (_children == null)
            {
                _children = new ObservableCollection<object>();
                // 초기에는 모든 네임스페이스 노드가 확장 가능하도록 더미 아이템 추가
                if (!_isLoaded && IsNamespace)
                {
                    _children.Add(DummyChild);
                }
            }
            return _children;
        }
        private set => SetProperty(ref _children, value);
    }

    private static readonly object DummyChild = new object();

    public void SetFilteredTypes(IEnumerable<Type>? types)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            _filteredTypes = types?.ToList(); // ToList()로 변환하여 재사용 가능하게 함
            _hasMatchedTypesCache = null;

            // 모든 하위 네임스페이스의 상태를 먼저 업데이트
            UpdateNodeState(types);

            // UI 업데이트를 단일 작업으로 처리
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        UpdateUIHierarchy();
                    }
                    finally
                    {
                        _isUpdating = false;
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
        catch
        {
            _isUpdating = false;
            throw;
        }
    }

    private void UpdateNodeState(IEnumerable<Type>? types)
    {
        // 현재 노드의 필터링된 타입 설정
        _filteredTypes = types;
        _hasMatchedTypesCache = null;

        // 하위 네임스페이스 상태 업데이트
        foreach (var childNs in _childNamespaces)
        {
            childNs.UpdateNodeState(types);
        }

        // 현재 노드의 확장 상태 결정
        if (types != null && HasMatchedTypes())
        {
            _isExpanded = true;
        }
    }

    private void UpdateUIHierarchy()
    {
        if (_children == null) return;

        // 모든 업데이트를 한 번에 수집
        var allUpdates = new List<(NamespaceTypeNode Node, List<object> NewItems)>();
        CollectUIUpdates(this, allUpdates);

        // UI 스레드에서 한 번에 모든 업데이트 적용
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            foreach (var (node, newItems) in allUpdates)
            {
                // 기존 아이템과 새 아이템이 다른 경우에만 업데이트
                if (!ItemsEqual(node.Children, newItems))
                {
                    node.Children.Clear();
                    foreach (var item in newItems)
                    {
                        node.Children.Add(item);
                    }
                }
            }
        }), DispatcherPriority.Background);
    }

    private bool ItemsEqual(ObservableCollection<object> existing, List<object> newItems)
    {
        if (existing.Count != newItems.Count) return false;
        
        for (int i = 0; i < existing.Count; i++)
        {
            if (!ReferenceEquals(existing[i], newItems[i])) return false;
        }
        
        return true;
    }

    private void CollectUIUpdates(NamespaceTypeNode node, List<(NamespaceTypeNode Node, List<object> NewItems)> updates)
    {
        var newItems = new List<object>();

        // 타입들을 먼저 추가
        var typesToShow = node._filteredTypes != null 
            ? node.Types.Where(t => node._filteredTypes.Contains(t))
            : node.Types;
        newItems.AddRange(typesToShow);

        // 하위 네임스페이스 추가
        foreach (var childNs in node._childNamespaces)
        {
            if (node._filteredTypes == null || childNs.HasMatchedTypes())
            {
                newItems.Add(childNs);
                CollectUIUpdates(childNs, updates);
            }
        }

        updates.Add((node, newItems));
        node._isLoaded = true;
    }

    private void LoadChildren()
    {
        if (_isLoaded && _filteredTypes == null && Children.Count > 0) return;
        
        if (Application.Current != null && !_isFiltering)
        {
            Application.Current.Dispatcher.Invoke(UpdateUIHierarchy);
        }
    }

    public NamespaceTypeNode(string fullNamespace, bool isPluginOnly = false)
    {
        _isPluginOnly = isPluginOnly;
        FullNamespace = fullNamespace;
        Name = fullNamespace.Split('.').Last();
        IsNamespace = true;
    }

    public NamespaceTypeNode(string fullNamespace, IEnumerable<Type> types, bool isPluginOnly = false)
        : this(fullNamespace, isPluginOnly)
    {
        _types = new ObservableCollection<Type>(types.OrderBy(t => t.Name));
    }

    public void AddChild(NamespaceTypeNode childNode)
    {
        _childNamespaces.Add(childNode);
        
        if (_children == null)
        {
            _children = new ObservableCollection<object>();
            if (!_isLoaded && IsNamespace)
            {
                _children.Add(DummyChild);
            }
        }
        
        // 이미 로드된 상태라면 자식 노드들을 다시 정렬하여 표시
        if (_isLoaded)
        {
            LoadChildren();
        }
    }

    public bool HasMatchedTypes()
    {
        // 캐시된 결과가 있으면 반환
        if (_hasMatchedTypesCache.HasValue)
            return _hasMatchedTypesCache.Value;

        // 필터링이 없는 경우
        if (_filteredTypes == null)
        {
            _hasMatchedTypesCache = true;
            return true;
        }

        bool hasMatches = false;
        
        // 현재 네임스페이스의 타입들 확인
        hasMatches = Types.Any(t => _filteredTypes.Contains(t));
        
        // 매칭된 타입이 없으면 하위 네임스페이스 확인
        if (!hasMatches)
        {
            hasMatches = _childNamespaces.Any(n => n.HasMatchedTypes());
        }

        _hasMatchedTypesCache = hasMatches;
        return hasMatches;
    }

    private IEnumerable<Type> GetAllTypes()
    {
        if (_isPluginOnly)
        {
            return NodeServices.PluginService.NodeTypes;
        }

        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    return Array.Empty<Type>();
                }
            })
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsInterface);
    }

    public static void ClearCache()
    {
        _cache.Clear();
    }

    public static IEnumerable<NamespaceTypeNode> CreateRootNodes(bool isPluginOnly = false)
    {
        var allTypes = (isPluginOnly ? 
            NodeServices.PluginService.NodeTypes : 
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsInterface))
                .OrderBy(t => t.Namespace?.Count(c => c == '.') ?? -1) // 네임스페이스 깊이로 1차 정렬
                .ThenBy(t => t.Namespace ?? "")                        // 네임스페이스 이름으로 2차 정렬
                .ThenBy(t => t.Name)                                   // 타입 이름으로 3차 정렬
                .ToList();

        // 네임스페이스가 없는 타입들 먼저 처리 (깊이가 -1이므로 자동으로 먼저 처리됨)
        var noNamespaceTypes = allTypes
            .Where(t => string.IsNullOrEmpty(t.Namespace))
            .ToList();

        if (noNamespaceTypes.Any())
        {
            yield return new NamespaceTypeNode("(No Namespace)", noNamespaceTypes, isPluginOnly);
        }

        var namespaceHierarchy = new Dictionary<string, NamespaceTypeNode>();

        // 이미 깊이 순으로 정렬된 타입들을 순회하면서 네임스페이스 계층 구조 구성
        foreach (var type in allTypes.Where(t => !string.IsNullOrEmpty(t.Namespace)))
        {
            var namespaceParts = type.Namespace!.Split('.');
            var currentPath = "";

            for (int i = 0; i < namespaceParts.Length; i++)
            {
                var part = namespaceParts[i];
                currentPath = i == 0 ? part : $"{currentPath}.{part}";

                if (!namespaceHierarchy.TryGetValue(currentPath, out var namespaceNode))
                {
                    namespaceNode = new NamespaceTypeNode(currentPath, isPluginOnly);
                    namespaceHierarchy[currentPath] = namespaceNode;

                    // 부모 네임스페이스가 있으면 자식으로 추가
                    if (i > 0)
                    {
                        var parentPath = string.Join(".", namespaceParts.Take(i));
                        if (namespaceHierarchy.TryGetValue(parentPath, out var parentNode))
                        {
                            parentNode.AddChild(namespaceNode);
                        }
                    }
                }

                // 현재 네임스페이스에 해당하는 타입 추가
                if (i == namespaceParts.Length - 1)
                {
                    namespaceNode.Types.Add(type);
                }
            }
        }

        // 최상위 네임스페이스만 반환 (이미 정렬되어 있으므로 추가 정렬 불필요)
        foreach (var node in namespaceHierarchy.Values.Where(n => !n.FullNamespace.Contains('.')))
        {
            yield return node;
        }
    }
} 
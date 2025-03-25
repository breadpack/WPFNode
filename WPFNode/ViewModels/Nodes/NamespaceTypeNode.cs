using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using WPFNode.Services;
using WPFNode.Interfaces;
using WPFNode.Utilities;
using WPFNode.Controls;
using System.Windows;
using System.Windows.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

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
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged(nameof(IsExpanded));
            OnPropertyChanged(nameof(Children));
        }
    }

    public ObservableCollection<object> Children 
    { 
        get
        {
            if (_children == null)
            {
                _children = new ObservableCollection<object>();
            }
            return _children;
        }
        private set => SetProperty(ref _children, value);
    }

    public void SetFilteredTypes(IEnumerable<Type>? types)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            // 병렬 처리로 전체 트리의 필터와 캐시를 초기화
            // ConcurrentBag은 스레드 안전한 컬렉션
            var typesCollection = types != null ? new HashSet<Type>(types) : null;
            
            // 필터 설정
            _filteredTypes = typesCollection;
            _hasMatchedTypesCache = null;
            
            // 모든 하위 네임스페이스에 대해 병렬로 필터 초기화
            if (_childNamespaces.Count > 0)
            {
                Parallel.ForEach(_childNamespaces, childNs =>
                {
                    childNs.SetFilteredTypesInternal(typesCollection);
                });
            }

            // 하위에서 상위로 HasMatchedTypes 결과를 계산
            UpdateMatchedTypesCache();

            // 검색 중이고 매칭되는 타입이 있는 경우에만 확장
            IsExpanded = _filteredTypes != null && _hasMatchedTypesCache == true;

            // UI 업데이트 수행
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    UpdateUIHierarchy();
                }
                finally
                {
                    _isUpdating = false;
                }
            }), DispatcherPriority.Background);
        }
        catch
        {
            _isUpdating = false;
            throw;
        }
    }

    private void SetFilteredTypesInternal(IEnumerable<Type>? types)
    {
        _filteredTypes = types?.ToList();
        _hasMatchedTypesCache = null;

        // 하위 네임스페이스의 필터와 캐시 초기화
        foreach (var childNs in _childNamespaces)
        {
            childNs.SetFilteredTypesInternal(types);
        }
    }

    private void UpdateMatchedTypesCache()
    {
        // 먼저 모든 하위 네임스페이스의 캐시를 업데이트
        foreach (var childNs in _childNamespaces)
        {
            childNs.UpdateMatchedTypesCache();
        }

        // 필터링이 없는 경우
        if (_filteredTypes == null)
        {
            _hasMatchedTypesCache = true;
            return;
        }

        // 현재 네임스페이스의 타입들 확인
        var hasMatchingTypes = Types.Any(t => _filteredTypes.Contains(t));
        
        // 하위 네임스페이스 확인 (이미 계산된 캐시 사용)
        var hasMatchingChildren = _childNamespaces.Any(childNs => childNs._hasMatchedTypesCache == true);

        // 결과 캐시
        _hasMatchedTypesCache = hasMatchingTypes || hasMatchingChildren;
    }

    public bool HasMatchedTypes()
    {
        // 캐시된 결과가 없으면 계산
        if (!_hasMatchedTypesCache.HasValue)
        {
            UpdateMatchedTypesCache();
        }
        return _hasMatchedTypesCache.Value;
    }

    private void InvalidateCache()
    {
        _hasMatchedTypesCache = null;
        foreach (var childNs in _childNamespaces)
        {
            childNs.InvalidateCache();
        }
    }

    private bool SequenceEqual(IEnumerable<Type>? first, IEnumerable<Type>? second)
    {
        if (first == null && second == null) return true;
        if (first == null || second == null) return false;
        return first.SequenceEqual(second);
    }

    private string? GetParentNamespace(string fullNamespace)
    {
        var lastDotIndex = fullNamespace.LastIndexOf('.');
        return lastDotIndex > 0 ? fullNamespace.Substring(0, lastDotIndex) : null;
    }

    private NamespaceTypeNode? FindParentNode(string parentNamespace)
    {
        // 루트 노드들에서 부모 네임스페이스 찾기
        var rootNodes = GetRootNodes();
        foreach (var node in rootNodes)
        {
            if (node.FullNamespace == parentNamespace)
            {
                return node;
            }

            var found = FindParentNodeRecursive(node, parentNamespace);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private NamespaceTypeNode? FindParentNodeRecursive(NamespaceTypeNode current, string targetNamespace)
    {
        if (current.FullNamespace == targetNamespace)
        {
            return current;
        }

        foreach (var child in current._childNamespaces)
        {
            var found = FindParentNodeRecursive(child, targetNamespace);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private IEnumerable<NamespaceTypeNode> GetRootNodes()
    {
        // 현재 노드가 속한 루트 노드들 찾기
        var current = this;
        while (true)
        {
            var parent = FindParentNode(GetParentNamespace(current.FullNamespace) ?? "");
            if (parent == null)
            {
                // 현재 노드가 속한 트리의 모든 루트 노드 반환
                return GetAllRootNodes();
            }
            current = parent;
        }
    }

    private IEnumerable<NamespaceTypeNode> GetAllRootNodes()
    {
        // TypeSelectorDialog에서 설정한 _allNodes를 찾아서 반환
        var field = typeof(TypeSelectorDialog).GetField("_allNodes", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            var dialog = Application.Current.Windows.OfType<TypeSelectorDialog>().FirstOrDefault();
            if (dialog != null)
            {
                return (field.GetValue(dialog) as IEnumerable<NamespaceTypeNode>) ?? [];
            }
        }
        return [];
    }

    private void CollectUIUpdates(NamespaceTypeNode node, List<(NamespaceTypeNode Node, List<object> NewItems)> updates, HashSet<NamespaceTypeNode>? visited = null)
    {
        visited ??= new HashSet<NamespaceTypeNode>();
        
        // 순환 참조 체크
        if (!visited.Add(node)) return;

        var newItems = new List<object>();

        // 현재 네임스페이스의 타입들 필터링
        var typesToShow = node._filteredTypes != null 
            ? node.Types.Where(t => node._filteredTypes.Contains(t))
            : node.Types;
        newItems.AddRange(typesToShow);

        // 하위 네임스페이스 처리
        foreach (var childNs in node._childNamespaces)
        {
            // 하위 네임스페이스가 매칭된 항목을 가지고 있는 경우에만 추가
            if (childNs.HasMatchedTypes())
            {
                newItems.Add(childNs);
                CollectUIUpdates(childNs, updates, visited);
            }
        }

        // 매칭된 항목이 있는 경우에만 업데이트 목록에 추가
        if (newItems.Any())
        {
            updates.Add((node, newItems));
            node._isLoaded = true;
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
            try
            {
                foreach (var (node, newItems) in allUpdates)
                {
                    // 기존 아이템과 새 아이템이 다른 경우에만 업데이트
                    if (!ItemsEqual(node.Children, newItems))
                    {
                        var children = node.Children;
                        var tempList = newItems.ToList(); // 새 아이템의 복사본 생성

                        // 한 번의 Clear와 AddRange로 처리
                        children.Clear();
                        foreach (var item in tempList)
                        {
                            children.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 디버깅을 위한 예외 로깅
                System.Diagnostics.Debug.WriteLine($"UI 업데이트 중 오류 발생: {ex}");
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

    public IEnumerable<Type> GetAllTypesInHierarchy()
    {
        // 현재 네임스페이스의 타입들
        foreach (var type in Types)
        {
            yield return type;
        }

        // 하위 네임스페이스의 타입들을 재귀적으로 수집
        foreach (var childNs in _childNamespaces)
        {
            foreach (var type in childNs.GetAllTypesInHierarchy())
            {
                yield return type;
            }
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
        }
        
        // 이미 로드된 상태라면 UI 업데이트
        if (_isLoaded && Application.Current != null)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(UpdateUIHierarchy), DispatcherPriority.Background);
        }
    }
}

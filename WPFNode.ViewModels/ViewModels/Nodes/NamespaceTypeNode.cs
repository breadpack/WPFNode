using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using WPFNode.Services;
using WPFNode.Interfaces;
using WPFNode.Utilities;
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
            // 필터가 없으면 빠르게 모든 타입 표시
            if (types == null)
            {
                _filteredTypes = null;
                _hasMatchedTypesCache = true;
                
                // 모든 자식 노드에 대해서도 동일하게 설정
                foreach (var childNs in _childNamespaces)
                {
                    childNs._filteredTypes = null;
                    childNs._hasMatchedTypesCache = true;
                }
                
                // UI 업데이트는 필요한 경우만 수행
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            // 전체 트리 표시
                            if (_children != null)
                            {
                                _children.Clear();
                                PopulateChildrenWithoutFiltering();
                            }
                        }
                        finally
                        {
                            _isUpdating = false;
                        }
                    }), DispatcherPriority.Background);
                }
                else
                {
                    _isUpdating = false;
                }
                
                return;
            }
            
            // 효율적인 검색을 위해 HashSet으로 변환
            var typesSet = new HashSet<Type>(types);
            _filteredTypes = typesSet;
            _hasMatchedTypesCache = null;
            
            // 최적화된 필터링: 타입이 있는 노드만 재귀 처리, 직접 매칭 우선 확인
            bool hasDirectMatches = false;
            foreach (var type in Types)
            {
                if (typesSet.Contains(type))
                {
                    hasDirectMatches = true;
                    break; // 하나라도 매치되면 중단
                }
            }
            
            // 자식 노드 필터링 최적화
            bool hasMatchingChildren = false;
            
            if (_childNamespaces.Count > 0)
            {
                // 병렬 처리로 성능 향상 (노드가 많을 경우)
                if (_childNamespaces.Count > 10)
                {
                    Parallel.ForEach(_childNamespaces, childNs =>
                    {
                        // 자식이 많거나 직접 매치되는 타입이 있는 경우만 재귀 호출
                        if (childNs._childNamespaces.Count > 0 || 
                            childNs.Types.Any(t => typesSet.Contains(t)))
                        {
                            childNs.SetFilteredTypesInternal(typesSet);
                            
                            // 스레드 안전한 방식으로 결과 확인
                            if (childNs._hasMatchedTypesCache == true)
                            {
                                // bool 대신 int 플래그 사용
                                int flag = 1; // true를 의미
                                Interlocked.Exchange(ref flag, 1);
                                hasMatchingChildren = true;
                            }
                        }
                        else
                        {
                            // 타입이 없는 경우 빠르게 처리
                            childNs._filteredTypes = typesSet;
                            childNs._hasMatchedTypesCache = false;
                        }
                    });
                }
                else
                {
                    // 노드가 적은 경우 일반 루프 사용
                    foreach (var childNs in _childNamespaces)
                    {
                        // 자식이 많거나 직접 매치되는 타입이 있는 경우만 재귀 호출
                        if (childNs._childNamespaces.Count > 0 || 
                            childNs.Types.Any(t => typesSet.Contains(t)))
                        {
                            childNs.SetFilteredTypesInternal(typesSet);
                            
                            // 일치하는 자식이 있으면 플래그 설정
                            if (childNs._hasMatchedTypesCache == true)
                            {
                                hasMatchingChildren = true;
                            }
                        }
                        else
                        {
                            // 타입이 없는 경우 빠르게 처리
                            childNs._filteredTypes = typesSet;
                            childNs._hasMatchedTypesCache = false;
                        }
                    }
                }
            }
            
            // 결과 캐시 직접 설정 (UpdateMatchedTypesCache 호출 생략)
            _hasMatchedTypesCache = hasDirectMatches || hasMatchingChildren;

            // 검색 중이고 매칭되는 타입이 있는 경우에만 확장
            IsExpanded = _hasMatchedTypesCache == true;

            // UI 업데이트는 별도 Dispatcher에서 배치 처리
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // 성능 최적화된 UI 업데이트
                        OptimizedUIUpdate(typesSet);
                    }
                    finally
                    {
                        _isUpdating = false;
                    }
                }), DispatcherPriority.Background);
            }
            else
            {
                _isUpdating = false;
            }
        }
        catch
        {
            _isUpdating = false;
            throw;
        }
    }
    
    // 필터가 없을 때 모든 자식 추가 (최적화)
    private void PopulateChildrenWithoutFiltering()
    {
        if (_children == null)
            _children = new ObservableCollection<object>();
            
        // 타입 추가
        foreach (var type in Types)
        {
            _children.Add(type);
        }
        
        // 네임스페이스 추가
        foreach (var ns in _childNamespaces)
        {
            _children.Add(ns);
        }
    }
    
    // UI 업데이트 최적화 버전
    private void OptimizedUIUpdate(HashSet<Type> typesSet)
    {
        if (_children == null)
            _children = new ObservableCollection<object>();
        
        // 배치 업데이트를 위해 새 컬렉션 준비
        var newItems = new List<object>();
        
        // 필터링된 타입 추가
        foreach (var type in Types)
        {
            if (typesSet.Contains(type))
            {
                newItems.Add(type);
            }
        }
        
        // 매치된 네임스페이스 추가
        foreach (var ns in _childNamespaces)
        {
            if (ns._hasMatchedTypesCache == true)
            {
                newItems.Add(ns);
            }
        }
        
        // Children 컬렉션을 한 번에 업데이트
        using (new DeferredCollectionChange(_children))
        {
            _children.Clear();
            foreach (var item in newItems)
            {
                _children.Add(item);
            }
        }
        
        _isLoaded = true;
    }

    private void SetFilteredTypesInternal(IEnumerable<Type>? types)
    {
        _filteredTypes = types;
        _hasMatchedTypesCache = null;

        // 항상 모든 자식 노드에 대해 재귀적으로 처리
        foreach (var childNs in _childNamespaces)
        {
            childNs.SetFilteredTypesInternal(types);
        }
        
        // 필터링 결과 캐시 업데이트
        // 현재 네임스페이스의 타입들과 필터링된 타입의 교집합 존재 여부 확인
        if (types != null && Types.Count > 0)
        {
            var typesSet = types as HashSet<Type> ?? new HashSet<Type>(types);
            var hasMatches = Types.Any(t => typesSet.Contains(t));
            
            // 자식 노드들의 매칭 결과도 고려
            var hasMatchingChildren = _childNamespaces.Any(childNs => childNs.HasMatchedTypes());
            
            _hasMatchedTypesCache = hasMatches || hasMatchingChildren;
        }
        else
        {
            // 필터링 없을 때는 모든 노드 표시
            _hasMatchedTypesCache = true;
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

    // 부모 노드 참조 추가 - 트리 탐색 최적화
    private NamespaceTypeNode? _parent;
    
    // 네임스페이스 캐시 - 빠른 조회를 위한 정적 사전
    private static readonly Dictionary<string, NamespaceTypeNode> _namespaceCache = new();
    
    private NamespaceTypeNode? FindParentNode(string parentNamespace)
    {
        // 부모 참조가 이미 있으면 사용
        if (_parent != null) return _parent;
        
        // 캐시에서 찾기
        if (_namespaceCache.TryGetValue(parentNamespace, out var node))
        {
            return node;
        }
        
        // 캐시에 없으면 null 반환
        return null;
    }
    
    // 불필요한 재귀 호출 제거
    private IEnumerable<NamespaceTypeNode> GetRootNodes()
    {
        // 루트 노드이거나 부모가 없으면 자신을 포함한 같은 레벨의 루트 노드들 반환
        if (_parent == null)
        {
            // 캐시에서 최상위 노드들 찾기
            var roots = _namespaceCache.Values.Where(n => !n.FullNamespace.Contains('.'));
            
            if (roots.Any())
            {
                return roots;
            }
            else
            {
                return new[] { this }; // 캐시가 비어있으면 자신만 반환
            }
        }
        
        // 부모가 있으면 부모의 루트 노드 반환
        var current = this;
        while (current._parent != null)
        {
            current = current._parent;
        }
        
        return new[] { current };
    }
    
    // Reflection 사용 제거
    private IEnumerable<NamespaceTypeNode> GetAllRootNodes()
    {
        // 캐시에서 모든 루트 노드 반환
        var roots = _namespaceCache.Values.Where(n => !n.FullNamespace.Contains('.'));
        if (roots.Any())
        {
            return roots;
        }
        
        // 캐시가 비어있으면 자신만 반환
        return new[] { this };
    }

private void CollectUIUpdates(NamespaceTypeNode node, List<(NamespaceTypeNode Node, List<object> NewItems)> updates, HashSet<NamespaceTypeNode>? visited = null)
{
    visited ??= new HashSet<NamespaceTypeNode>();
    
    // 순환 참조 체크
    if (!visited.Add(node)) return;

    // 현재 노드의 아이템 처리
    var newItems = new List<object>();

    // 현재 네임스페이스의 타입들 필터링 - 효율적인 필터링
    var typesToShow = node._filteredTypes != null 
        ? node.Types.Intersect(node._filteredTypes).ToList() // HashSet.Intersect 사용
        : node.Types.ToList();
    
    if (typesToShow.Count > 0)
    {
        newItems.AddRange(typesToShow);
    }

    // 하위 네임스페이스 처리 - 재귀 호출 복원
    foreach (var childNs in node._childNamespaces)
    {
        // 하위 네임스페이스가 매칭된 항목을 가지고 있는 경우에만 추가
        if (childNs.HasMatchedTypes())
        {
            newItems.Add(childNs);
            
            // 하위 노드도 재귀적으로 처리 - 깊은 레벨의 노드도 처리하기 위함
            CollectUIUpdates(childNs, updates, visited);
        }
    }

    // 매칭된 항목이 있는 경우에만 업데이트 목록에 추가
    if (newItems.Count > 0)
    {
        updates.Add((node, newItems));
        node._isLoaded = true;
    }
}

private void UpdateUIHierarchy()
{
    if (_children == null) return;

    // 전체 트리를 재귀적으로 처리
    var allUpdates = new List<(NamespaceTypeNode Node, List<object> NewItems)>();
    
    // 재귀 호출로 전체 트리 구조 처리
    CollectUIUpdates(this, allUpdates);
    
    // UI 스레드에서 한 번에 모든 업데이트 적용 - 배치 업데이트
    if (allUpdates.Count > 0 && Application.Current != null)
    {
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                foreach (var (node, items) in allUpdates)
                {
                    // 로컬 변수 사용으로 불필요한 속성 접근 최소화
                    var children = node.Children;
                    
                    // 복사 없이 직접 업데이트
                    children.Clear();
                    foreach (var item in items)
                    {
                        children.Add(item);
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
        
        // 부모-자식 관계 설정 (최적화된 트리 탐색을 위함)
        childNode._parent = this;
        
        // 네임스페이스 캐시에 추가 (빠른 조회를 위함)
        _namespaceCache[childNode.FullNamespace] = childNode;
        
        if (_children == null)
        {
            _children = new ObservableCollection<object>();
        }
        
        // 이미 로드된 상태라면 UI 업데이트
        if (_isLoaded && Application.Current != null)
        {
            // UI 업데이트 최적화: 전체 트리 대신 현재 노드만 업데이트
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                if (_isExpanded)
                {
                    // 자식이 추가되었을 때 확장된 상태라면 자식 노드 표시
                    if (!_children.Contains(childNode))
                    {
                        _children.Add(childNode);
                    }
                }
            }), DispatcherPriority.Background);
        }
    }
    
    // 초기화 시 캐시에 자기 자신 등록
    static NamespaceTypeNode()
    {
        // 클래스 초기화 시 캐시 초기화
        _namespaceCache.Clear();
    }
}

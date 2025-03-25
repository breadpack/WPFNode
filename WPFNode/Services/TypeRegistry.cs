using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WPFNode.ViewModels.Nodes;

namespace WPFNode.Services;

/// <summary>
/// 애플리케이션의 타입 정보를 캐싱하고 빠른 검색 기능을 제공하는 싱글톤 서비스
/// </summary>
public class TypeRegistry
{
    private static TypeRegistry? _instance;
    private static readonly object _lockObject = new();

    // 싱글톤 인스턴스
    public static TypeRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lockObject)
                {
                    _instance ??= new TypeRegistry();
                }
            }
            return _instance;
        }
    }

    // 전체 타입 컬렉션
    private List<Type> _allTypes = new();
    private List<Type> _pluginTypes = new();
    
    // 네임스페이스 트리 (미리 구성된 NamespaceTypeNode 트리)
    private List<NamespaceTypeNode> _rootNamespaceNodes = new();
    private List<NamespaceTypeNode> _pluginNamespaceNodes = new();
    
    // 검색 인덱스
    private Dictionary<string, HashSet<Type>> _wordToTypesIndex = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, HashSet<Type>> _namespaceToTypesIndex = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, HashSet<Type>> _prefixToTypesIndex = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, HashSet<Type>> _acronymToTypesIndex = new(StringComparer.OrdinalIgnoreCase);
    
    // 초기화 상태
    private bool _isInitialized;
    private readonly object _initLock = new();
    private readonly TaskCompletionSource<bool> _initializationCompletionSource = new();
    
    // 백그라운드 작업 취소용 토큰
    private CancellationTokenSource? _cts;

    private TypeRegistry()
    {
        // 프라이빗 생성자 - 싱글톤 패턴
    }

    /// <summary>
    /// TypeRegistry를 비동기적으로 초기화합니다.
    /// </summary>
    /// <returns>초기화 완료를 나타내는 Task</returns>
    public Task InitializeAsync()
    {
        // 이미 초기화 중이거나 완료된 경우 기존 작업 반환
        if (_isInitialized)
            return Task.CompletedTask;
            
        if (_initializationCompletionSource.Task.Status == TaskStatus.RanToCompletion)
            return _initializationCompletionSource.Task;
            
        lock (_initLock)
        {
            if (_isInitialized || _initializationCompletionSource.Task.Status == TaskStatus.RanToCompletion) 
                return _initializationCompletionSource.Task;
            
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            
            // 백그라운드 스레드에서 초기화 작업 수행
            Task.Run(() => InitializeInternal(token), token)
                .ContinueWith(t => {
                    if (t.IsFaulted)
                    {
                        if (t.Exception?.InnerExceptions != null)
                            _initializationCompletionSource.TrySetException(t.Exception.InnerExceptions);
                        else
                            _initializationCompletionSource.TrySetException(new Exception("TypeRegistry 초기화 실패"));
                    }
                    else if (t.IsCanceled)
                    {
                        _initializationCompletionSource.TrySetCanceled();
                    }
                    else
                    {
                        _isInitialized = true;
                        _initializationCompletionSource.TrySetResult(true);
                    }
                });
                
            return _initializationCompletionSource.Task;
        }
    }

    /// <summary>
    /// 전체 네임스페이스 노드 목록을 반환합니다.
    /// </summary>
    public IEnumerable<NamespaceTypeNode> GetNamespaceNodes()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("TypeRegistry가 아직 초기화되지 않았습니다. InitializeAsync()를 먼저 호출하세요.");
            
        return _rootNamespaceNodes;
    }

    /// <summary>
    /// 플러그인 네임스페이스 노드 목록을 반환합니다.
    /// </summary>
    public IEnumerable<NamespaceTypeNode> GetPluginNamespaceNodes()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("TypeRegistry가 아직 초기화되지 않았습니다. InitializeAsync()를 먼저 호출하세요.");
            
        return _pluginNamespaceNodes;
    }

    /// <summary>
    /// 주어진 쿼리로 타입을 검색합니다.
    /// </summary>
    /// <param name="query">검색어</param>
    /// <param name="pluginOnly">플러그인 타입만 검색할지 여부</param>
    /// <returns>검색 결과 타입 목록</returns>
    public List<Type> SearchTypes(string query, bool pluginOnly = false)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("TypeRegistry가 아직 초기화되지 않았습니다. InitializeAsync()를 먼저 호출하세요.");
            
        var sourceTypes = pluginOnly ? _pluginTypes : _allTypes;
        
        if (string.IsNullOrWhiteSpace(query))
            return sourceTypes.ToList();
            
        var terms = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.ToLowerInvariant())
                        .ToArray();
        
        if (terms.Length == 0)
            return sourceTypes.ToList();
        
        // 첫 번째 검색어로 후보 집합 초기화
        HashSet<Type> resultSet = GetCandidatesForTerm(terms[0], sourceTypes);
        
        // 나머지 검색어로 결과 필터링 (AND 연산)
        for (int i = 1; i < terms.Length; i++)
        {
            var termCandidates = GetCandidatesForTerm(terms[i], sourceTypes);
            resultSet.IntersectWith(termCandidates);
            
            if (resultSet.Count == 0)
                break; // 더 이상 매칭되는 것이 없으면 중단
        }
        
        // 정렬 알고리즘 적용 (정확도 기준)
        return resultSet.OrderBy(t => t.Name).ToList();
    }

    /// <summary>
    /// 백그라운드에서 초기화 작업을 수행합니다.
    /// </summary>
    private void InitializeInternal(CancellationToken token)
    {
        try
        {
            // 1. 타입 로딩
            LoadAllTypes(token);
            
            // 2. 네임스페이스 트리 구성
            BuildNamespaceTree(token);
            
            // 3. 검색 인덱스 구축
            BuildSearchIndices(token);
        }
        catch (OperationCanceledException)
        {
            // 작업 취소됨
            throw;
        }
        catch (Exception ex)
        {
            // 로깅
            System.Diagnostics.Debug.WriteLine($"TypeRegistry 초기화 실패: {ex}");
            throw;
        }
    }

    /// <summary>
    /// 모든 타입을 로드합니다.
    /// </summary>
    private void LoadAllTypes(CancellationToken token)
    {
        // 시스템 어셈블리 캐싱
        var systemAssemblies = new HashSet<string> {
            "mscorlib", "System", "System.Core", "WindowsBase", "PresentationCore", "PresentationFramework"
        };
        
        // 병렬 처리용 옵션 추가: MaxDegreeOfParallelism 설정
        var options = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = Environment.ProcessorCount // 코어 수에 맞춰 조정
        };
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .ToList();
            
        // 플러그인 타입과 어셈블리
        var pluginTypes = NodeServices.PluginService.NodeTypes;
        var pluginAssemblies = pluginTypes
            .Select(t => t.Assembly)
            .Distinct()
            .ToHashSet();
        
        // 타입 로딩 - 스레드 안전한 컬렉션 사용
        var allTypesList = new ConcurrentBag<Type>();
        var pluginTypesList = new ConcurrentBag<Type>();
        
        // 병렬 옵션 적용
        Parallel.ForEach(assemblies, options, assembly => {
            if (token.IsCancellationRequested) return;
            
            try {
                var types = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsInterface)
                    .ToList();
                    
                foreach (var type in types)
                {
                    allTypesList.Add(type);
                    
                    // 플러그인 타입 구분
                    if (pluginAssemblies.Contains(assembly))
                        pluginTypesList.Add(type);
                }
            }
            catch (ReflectionTypeLoadException) { }
        });
        
        // 결과 정렬 - 병렬 처리 후 결과가 일관되도록
        _allTypes = allTypesList.OrderBy(t => t.FullName).ToList();
        _pluginTypes = pluginTypesList.OrderBy(t => t.FullName).ToList();
    }

    /// <summary>
    /// 네임스페이스 트리를 구성합니다.
    /// </summary>
    private void BuildNamespaceTree(CancellationToken token)
    {
        // 네임스페이스별로 타입을 그룹화하는 작업을 병렬로 수행
        var allNamespaceGroups = _allTypes
            .AsParallel()
            .WithCancellation(token)
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .GroupBy(t => t.Namespace ?? "(No Namespace)")
            .ToDictionary(g => g.Key, g => g.ToList());
            
        var pluginNamespaceGroups = _pluginTypes
            .AsParallel()
            .WithCancellation(token)
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .GroupBy(t => t.Namespace ?? "(No Namespace)")
            .ToDictionary(g => g.Key, g => g.ToList());
        
        // 네임스페이스 트리 생성 - 병렬 처리로 변경
        _rootNamespaceNodes = CreateNamespaceNodes(_allTypes, token).ToList();
        _pluginNamespaceNodes = CreateNamespaceNodes(_pluginTypes, token).ToList();
    }

    /// <summary>
    /// 검색 인덱스를 구축합니다.
    /// </summary>
    private void BuildSearchIndices(CancellationToken token)
    {
        // 병렬 처리용 옵션
        var options = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };
        
        // 스레드 안전한 임시 인덱스 컬렉션
        var wordIndex = new ConcurrentDictionary<string, ConcurrentBag<Type>>(StringComparer.OrdinalIgnoreCase);
        var prefixIndex = new ConcurrentDictionary<string, ConcurrentBag<Type>>(StringComparer.OrdinalIgnoreCase);
        var acronymIndex = new ConcurrentDictionary<string, ConcurrentBag<Type>>(StringComparer.OrdinalIgnoreCase);
        var namespaceIndex = new ConcurrentDictionary<string, ConcurrentBag<Type>>(StringComparer.OrdinalIgnoreCase);
        
        // 타입별 병렬 처리
        Parallel.ForEach(
            Partitioner.Create(0, _allTypes.Count),
            options,
            (range, state) => {
                if (token.IsCancellationRequested) state.Break();
                
                // 각 파티션 내에서 타입 처리
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var type = _allTypes[i];
                    
                    // 1. 타입 이름 분석
                    string typeName = type.Name;
                    string namespaceName = type.Namespace ?? "";
                    
                    // 2. 단어 추출 및 인덱싱
                    // 파스칼 케이스 단어 분리
                    var words = SplitPascalCase(typeName);
                    foreach (var word in words)
                    {
                        if (word.Length <= 1) continue; // 단일 문자 단어는 제외
                        
                        var lowerWord = word.ToLowerInvariant();
                        AddToConcurrentIndex(wordIndex, lowerWord, type);
                    }
                    
                    // 접두사 인덱싱 (성능을 위해 일부만 인덱싱)
                    for (int prefixLen = 1; prefixLen <= Math.Min(typeName.Length, 4); prefixLen++)
                    {
                        var prefix = typeName.Substring(0, prefixLen).ToLowerInvariant();
                        AddToConcurrentIndex(prefixIndex, prefix, type);
                    }
                    
                    // 약어 생성 및 인덱싱
                    string acronym = string.Concat(words.Select(w => w.FirstOrDefault()));
                    if (acronym.Length > 1)
                    {
                        var lowerAcronym = acronym.ToLowerInvariant();
                        AddToConcurrentIndex(acronymIndex, lowerAcronym, type);
                    }
                    
                    // 네임스페이스 인덱싱
                    var nsParts = namespaceName.Split('.');
                    foreach (var part in nsParts)
                    {
                        if (string.IsNullOrEmpty(part)) continue;
                        
                        var lowerPart = part.ToLowerInvariant();
                        AddToConcurrentIndex(namespaceIndex, lowerPart, type);
                    }
                }
            });
            
        // 스레드 안전한 인덱스에서 최종 인덱스로 변환
        if (!token.IsCancellationRequested)
        {
            _wordToTypesIndex = ConvertToDictionary(wordIndex);
            _prefixToTypesIndex = ConvertToDictionary(prefixIndex);
            _acronymToTypesIndex = ConvertToDictionary(acronymIndex);
            _namespaceToTypesIndex = ConvertToDictionary(namespaceIndex);
        }
    }

    /// <summary>
    /// 스레드 안전한 인덱스에 항목을 추가합니다.
    /// </summary>
    private void AddToConcurrentIndex(ConcurrentDictionary<string, ConcurrentBag<Type>> index, string key, Type type)
    {
        var bag = index.GetOrAdd(key, _ => new ConcurrentBag<Type>());
        bag.Add(type);
    }

    /// <summary>
    /// 인덱스에 항목을 추가합니다.
    /// </summary>
    private void AddToIndex(Dictionary<string, HashSet<Type>> index, string key, Type type)
    {
        if (!index.TryGetValue(key, out var typeSet))
        {
            typeSet = new HashSet<Type>();
            index[key] = typeSet;
        }
        typeSet.Add(type);
    }
    
    /// <summary>
    /// 스레드 안전 컬렉션에서 일반 Dictionary로 변환합니다.
    /// </summary>
    private Dictionary<string, HashSet<Type>> ConvertToDictionary(ConcurrentDictionary<string, ConcurrentBag<Type>> concurrent)
    {
        var result = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var pair in concurrent)
        {
            result[pair.Key] = new HashSet<Type>(pair.Value);
        }
        
        return result;
    }

    /// <summary>
    /// 한 검색어에 대한 후보 타입 집합을 반환합니다.
    /// </summary>
    private HashSet<Type> GetCandidatesForTerm(string term, IEnumerable<Type> sourceTypes)
    {
        var result = new HashSet<Type>();
        
        // 1. 정확한 단어 매칭 (가장 높은 우선순위)
        if (_wordToTypesIndex.TryGetValue(term, out var wordMatches))
            result.UnionWith(wordMatches.Intersect(sourceTypes));
        
        // 2. 접두사 매칭
        if (_prefixToTypesIndex.TryGetValue(term, out var prefixMatches))
            result.UnionWith(prefixMatches.Intersect(sourceTypes));
        
        // 3. 네임스페이스 매칭
        if (_namespaceToTypesIndex.TryGetValue(term, out var nsMatches))
            result.UnionWith(nsMatches.Intersect(sourceTypes));
        
        // 4. 약어 매칭 (대문자 검색어인 경우만)
        if (term.All(char.IsUpper) && _acronymToTypesIndex.TryGetValue(term.ToLowerInvariant(), out var acronymMatches))
            result.UnionWith(acronymMatches.Intersect(sourceTypes));
        
        // 모든 인덱스에서 찾지 못한 경우 부분 문자열 검색
        if (result.Count == 0)
        {
            // 부분 문자열 검색은 비용이 크므로 마지막 수단으로만 사용
            result.UnionWith(sourceTypes.Where(t => 
                t.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (t.Namespace?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0));
        }
        
        return result;
    }

    /// <summary>
    /// 네임스페이스 노드를 생성합니다.
    /// </summary>
    private IEnumerable<NamespaceTypeNode> CreateNamespaceNodes(IEnumerable<Type> types, CancellationToken token)
    {
        var namespaceGroups = types.GroupBy(t => t.Namespace ?? "(No Namespace)");
        var nodes = new Dictionary<string, NamespaceTypeNode>();

        foreach (var group in namespaceGroups)
        {
            if (token.IsCancellationRequested) break;
            
            var ns = group.Key;
            var parts = ns.Split('.');
            var currentNs = "";

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var fullNs = i == 0 ? part : $"{currentNs}.{part}";

                if (!nodes.ContainsKey(fullNs))
                {
                    var node = new NamespaceTypeNode(fullNs);
                    nodes[fullNs] = node;

                    if (i > 0 && nodes.TryGetValue(currentNs, out var parentNode))
                    {
                        parentNode.AddChild(node);
                    }
                }

                if (i == parts.Length - 1)
                {
                    foreach (var type in group.OrderBy(t => t.Name))
                    {
                        nodes[fullNs].Types.Add(type);
                    }
                }

                currentNs = fullNs;
            }
        }

        // 최상위 노드만 반환 (부모가 없는 노드들)
        return nodes.Values.Where(n => !n.FullNamespace.Contains('.'));
    }

    /// <summary>
    /// 파스칼 케이스 문자열을 개별 단어로 분리합니다.
    /// </summary>
    private static IEnumerable<string> SplitPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            yield break;

        var currentWord = new StringBuilder(input[0].ToString());
        
        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]) && 
                (char.IsLower(input[i - 1]) || 
                 (i + 1 < input.Length && char.IsLower(input[i + 1]))))
            {
                yield return currentWord.ToString();
                currentWord.Clear();
            }
            currentWord.Append(input[i]);
        }
        
        if (currentWord.Length > 0)
            yield return currentWord.ToString();
    }
}

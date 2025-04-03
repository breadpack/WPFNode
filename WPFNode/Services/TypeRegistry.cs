using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
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
            
            try
            {
                // 동기식으로 직접 호출
                InitializeInternal(CancellationToken.None);
                _isInitialized = true;
                _initializationCompletionSource.TrySetResult(true);
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("TypeRegistry 초기화 작업이 취소되었습니다.");
                _initializationCompletionSource.TrySetCanceled();
                throw;
            }
            catch (Exception ex)
            {
                // 심각한 오류만 로깅
                System.Diagnostics.Debug.WriteLine($"TypeRegistry 초기화 중 오류 발생: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                
                try
                {
                    // 백업 초기화 시도 - 최소한의 필수 기능만 초기화
                    System.Diagnostics.Debug.WriteLine("최소 기능으로 TypeRegistry 초기화 시도 중...");
                    BackupInitialize();
                    
                    // 초기화 성공으로 간주 (부분적 기능만 가능하더라도)
                    _isInitialized = true;
                    _initializationCompletionSource.TrySetResult(true);
                }
                catch (Exception backupEx)
                {
                    System.Diagnostics.Debug.WriteLine($"백업 초기화도 실패: {backupEx.Message}");
                    
                    // 백업 초기화도 실패했지만, 빈 컬렉션이라도 설정하여 최소한의 앱 동작은 보장
                    _allTypes = new List<Type>();
                    _pluginTypes = new List<Type>();
                    _rootNamespaceNodes = new List<NamespaceTypeNode>();
                    _pluginNamespaceNodes = new List<NamespaceTypeNode>();
                    _wordToTypesIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
                    _prefixToTypesIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
                    _acronymToTypesIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
                    _namespaceToTypesIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
                    
                    // 그래도 초기화 성공으로 설정 (앱 사용은 가능)
                    _isInitialized = true;
                    _initializationCompletionSource.TrySetResult(true);
                }
            }
                    
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
        
        // 관련성 점수 계산 및 정렬 (Best fit 우선)
        return resultSet
            .Select(t => new { Type = t, Score = CalculateRelevanceScore(t, terms) })
            .OrderByDescending(item => item.Score) // 점수 내림차순 (높은 점수가 먼저)
            .ThenBy(item => item.Type.Name) // 동일 점수는 이름 오름차순
            .Select(item => item.Type)
            .ToList();
    }
    
    /// <summary>
    /// 검색어에 대한 타입의 관련성 점수를 계산합니다.
    /// </summary>
    /// <param name="type">검사할 타입</param>
    /// <param name="searchTerms">검색어 배열</param>
    /// <returns>관련성 점수 (높을수록 더 관련성 높음)</returns>
    private int CalculateRelevanceScore(Type type, string[] searchTerms)
    {
        int score = 0;
        string typeName = type.Name.ToLowerInvariant();
        string fullName = type.FullName?.ToLowerInvariant() ?? "";
        
        foreach (var term in searchTerms)
        {
            // 1. 정확한 타입 이름 일치 (최고 우선순위)
            if (typeName.Equals(term, StringComparison.OrdinalIgnoreCase))
                score += 100;
                
            // 2. 타입 이름이 검색어로 시작 (접두사 매칭)
            else if (typeName.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                score += 75;
                
            // 3. 단어 단위 일치 (타입 이름 내 단어가 검색어와 일치)
            else if (SplitPascalCase(type.Name).Any(word => 
                     word.Equals(term, StringComparison.OrdinalIgnoreCase)))
                score += 50;
                
            // 4. 타입 이름에 검색어 포함 (부분 문자열)
            else if (typeName.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 25;
                
            // 5. 네임스페이스에 검색어 포함
            if (fullName.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 15;
                
            // 6. 약어 매칭 (대문자만 모았을 때 검색어와 일치)
            string acronym = string.Concat(type.Name.Where(char.IsUpper)).ToLowerInvariant();
            if (acronym.Equals(term, StringComparison.OrdinalIgnoreCase))
                score += 40;
        }
        
        // 보너스: 짧은 타입 이름에 약간의 가중치 부여 (동일 점수일 때 간단한 이름 우선)
        score += Math.Max(0, 10 - typeName.Length);
        
        return score;
    }

    /// <summary>
    /// 백그라운드에서 초기화 작업을 수행합니다.
    /// </summary>
    private void InitializeInternal(CancellationToken token)
    {
        try
        {
            // 1. 타입 로딩 - 순차적 실행
            LoadAllTypes(token);
            
            if (token.IsCancellationRequested) return;
            
            // 2. 네임스페이스 트리 구성 - 순차적 실행
            BuildNamespaceTree(token);
            
            if (token.IsCancellationRequested) return;
            
            // 3. 검색 인덱스 구축 - 순차적 실행
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
        
        // 결과 저장용 컬렉션
        var allTypes = new List<Type>();
        var pluginTypesList = new List<Type>();
        
        try
        {
            // 플러그인 타입과 어셈블리 먼저 로드 (가장 중요한 타입들)
            try
            {
                var pluginTypes = NodeServices.ModelService.NodeTypes;
                
                if (pluginTypes != null)
                {
                    var pluginAssemblies = pluginTypes
                        .Where(t => t != null) // null 체크 추가
                        .Select(t => t.Assembly)
                        .Where(a => a != null) // null 체크 추가
                        .Distinct()
                        .ToHashSet();
                    
                    // 플러그인 타입 직접 추가 (확실히 포함되도록)
                    pluginTypesList.AddRange(pluginTypes.Where(t => t != null)); // null 체크 추가
                    allTypes.AddRange(pluginTypes.Where(t => t != null)); // null 체크 추가
                    
                    // 추가로 플러그인 어셈블리의 모든 타입 포함
                    foreach (var assembly in pluginAssemblies)
                    {
                        if (token.IsCancellationRequested) break;
                        
                        try
                        {
                            var types = assembly.GetTypes().ToList();
                            // 중복 제거하며 추가
                            var newPluginTypes = types.Except(pluginTypesList).ToList();
                            pluginTypesList.AddRange(newPluginTypes);
                            
                            // 전체 타입에도 추가
                            var newAllTypes = types.Except(allTypes).ToList();
                            allTypes.AddRange(newAllTypes);
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            // 로드할 수 있는 타입만 추가
                            if (ex.Types != null)
                            {
                                var validTypes = ex.Types.Where(t => t != null).ToList();
                                pluginTypesList.AddRange(validTypes.Except(pluginTypesList));
                                allTypes.AddRange(validTypes.Except(allTypes));
                            }
                            
                            // 로더 예외 세부 정보 기록
                            if (ex.LoaderExceptions != null)
                            {
                                foreach (var loaderEx in ex.LoaderExceptions.Where(le => le != null))
                                {
                                    System.Diagnostics.Debug.WriteLine($"플러그인 어셈블리 {assembly.GetName().Name} 타입 로드 예외: {loaderEx.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"플러그인 어셈블리 {assembly.GetName().Name} 처리 중 예외 발생: {ex.Message}");
                            // 개별 어셈블리 처리 실패는 무시하고 계속 진행
                        }
                    }
                    
                    try
                    {
                        // 필요한 어셈블리만 필터링 - 시스템 어셈블리 제외, 플러그인 어셈블리 제외(이미 처리됨)
                        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => a != null && !systemAssemblies.Contains(a.GetName().Name) && !pluginAssemblies.Contains(a))
                            .ToList();
                            
                        // 이제 나머지 어셈블리 처리
                        foreach (var assembly in assemblies)
                        {
                            if (token.IsCancellationRequested) break;
                            
                            try
                            {
                                // GetTypes() 호출 - 각 어셈블리에 대해 한 번만 수행
                                var types = assembly.GetTypes()
                                    .ToList();
                                
                                // 중복 제거하며 타입 정보 추가
                                var newTypes = types.Except(allTypes).ToList();
                                allTypes.AddRange(newTypes);
                            }
                            catch (ReflectionTypeLoadException ex)
                            {
                                // 로드할 수 있는 타입만 추가
                                if (ex.Types != null)
                                {
                                    var validTypes = ex.Types.Where(t => t != null).ToList();
                                    allTypes.AddRange(validTypes.Except(allTypes));
                                }
                                
                                // 로더 예외 세부 정보 기록
                                if (ex.LoaderExceptions != null)
                                {
                                    foreach (var loaderEx in ex.LoaderExceptions.Where(le => le != null))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"어셈블리 {assembly.GetName().Name} 타입 로드 예외: {loaderEx.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"어셈블리 {assembly.GetName().Name} 처리 중 예외 발생: {ex.Message}");
                                // 개별 어셈블리 처리 실패는 무시하고 계속 진행
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AppDomain.CurrentDomain.GetAssemblies() 처리 중 예외 발생: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("NodeServices.ModelService.NodeTypes가 null입니다.");
                    // 플러그인 타입을 로드할 수 없는 경우 일반 어셈블리 로드로 대체
                    LoadAssembliesWithoutPlugins(token, systemAssemblies, allTypes);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NodeServices.ModelService.NodeTypes 접근 중 예외 발생: {ex.Message}");
                // NodeServices.ModelService.NodeTypes 접근 실패 시 일반 어셈블리 로드로 대체
                LoadAssembliesWithoutPlugins(token, systemAssemblies, allTypes);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"타입 로드 중 예외 발생: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
        }
        
        // 결과 정렬 및 할당 (최소한의 타입이라도 제공)
        _allTypes = allTypes.Where(t => t != null).OrderBy(t => t.FullName).ToList();
        _pluginTypes = pluginTypesList.Where(t => t != null).OrderBy(t => t.FullName).ToList();
        
        // 디버그 정보
        System.Diagnostics.Debug.WriteLine($"로드된 전체 타입 수: {_allTypes.Count}, 플러그인 타입 수: {_pluginTypes.Count}");
    }
    
    /// <summary>
    /// 플러그인 없이 어셈블리에서 타입을 로드합니다.
    /// </summary>
    private void LoadAssembliesWithoutPlugins(CancellationToken token, HashSet<string> systemAssemblies, List<Type> allTypes)
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a != null && !systemAssemblies.Contains(a.GetName().Name))
                .ToList();
                
            // 이제 나머지 어셈블리 처리
            foreach (var assembly in assemblies)
            {
                if (token.IsCancellationRequested) break;
                
                try
                {
                    // GetTypes() 호출 - 각 어셈블리에 대해 한 번만 수행
                    var types = assembly.GetTypes().ToList();
                    
                    // 중복 제거하며 타입 정보 추가
                    var newTypes = types.Except(allTypes).ToList();
                    allTypes.AddRange(newTypes);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 로드할 수 있는 타입만 추가
                    if (ex.Types != null)
                    {
                        var validTypes = ex.Types.Where(t => t != null).ToList();
                        allTypes.AddRange(validTypes.Except(allTypes));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"어셈블리 {assembly.GetName().Name} 처리 중 예외 발생: {ex.Message}");
                    // 계속 진행
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadAssembliesWithoutPlugins 처리 중 예외 발생: {ex.Message}");
        }
    }

    /// <summary>
    /// 네임스페이스 트리를 구성합니다.
    /// </summary>
    private void BuildNamespaceTree(CancellationToken token)
    {
        // 네임스페이스별로 타입을 그룹화하는 작업을 순차적으로 수행
        var allNamespaceGroups = _allTypes
            .GroupBy(t => t.Namespace ?? "(No Namespace)")
            .ToDictionary(g => g.Key, g => g.ToList());
            
        if (token.IsCancellationRequested) return;
        
        var pluginNamespaceGroups = _pluginTypes
            .GroupBy(t => t.Namespace ?? "(No Namespace)")
            .ToDictionary(g => g.Key, g => g.ToList());
        
        if (token.IsCancellationRequested) return;
        
        // 네임스페이스 트리 생성 - 순차적으로 처리
        _rootNamespaceNodes = CreateNamespaceNodes(_allTypes, token).ToList();
        _pluginNamespaceNodes = CreateNamespaceNodes(_pluginTypes, token).ToList();
    }

    /// <summary>
    /// 검색 인덱스를 구축합니다.
    /// </summary>
    private void BuildSearchIndices(CancellationToken token)
    {
        // 각 인덱스를 위한 딕셔너리 직접 생성
        var wordIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
        var prefixIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
        var acronymIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
        var namespaceIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
        
        // 타입별 순차 처리 - 모든 타입과 플러그인 타입 통합 인덱스 구축
        var allTypesToIndex = new HashSet<Type>(_allTypes);
        allTypesToIndex.UnionWith(_pluginTypes); // 누락되는 플러그인 타입을 추가

        foreach (var type in allTypesToIndex)
        {
            if (token.IsCancellationRequested) break;
            
            // 1. 타입 이름 분석
            string typeName = type.Name;
            string namespaceName = type.Namespace ?? "";
            
            // 2. 단어 추출 및 인덱싱
            var words = SplitPascalCase(typeName);
            foreach (var word in words)
            {
                // 빈 단어가 아닌 경우 인덱싱 (길이 1도 허용)
                if (string.IsNullOrEmpty(word)) continue;
                
                var lowerWord = word.ToLowerInvariant();
                AddToIndex(wordIndex, lowerWord, type);
            }
            
            // 접두사 인덱싱
            for (int prefixLen = 2; prefixLen <= Math.Min(typeName.Length, 4); prefixLen++)
            {
                var prefix = typeName.Substring(0, prefixLen).ToLowerInvariant();
                AddToIndex(prefixIndex, prefix, type);
            }
            
            // 약어 생성 및 인덱싱
            string acronym = string.Concat(words.Select(w => w.FirstOrDefault()));
            if (acronym.Length > 1)
            {
                var lowerAcronym = acronym.ToLowerInvariant();
                AddToIndex(acronymIndex, lowerAcronym, type);
            }
            
            // 네임스페이스 인덱싱
            var nsParts = namespaceName.Split('.');
            foreach (var part in nsParts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                
                var lowerPart = part.ToLowerInvariant();
                AddToIndex(namespaceIndex, lowerPart, type);
            }
        }
        
        // 최종 인덱스 할당
        _wordToTypesIndex = wordIndex;
        _prefixToTypesIndex = prefixIndex;
        _acronymToTypesIndex = acronymIndex;
        _namespaceToTypesIndex = namespaceIndex;
    }
    
    // 로컬 인덱스에 항목을 추가하는 헬퍼 메소드
    private void AddToLocalIndex(Dictionary<string, HashSet<Type>> index, string key, Type type)
    {
        if (!index.TryGetValue(key, out var typeSet))
        {
            typeSet = new HashSet<Type>();
            index[key] = typeSet;
        }
        typeSet.Add(type);
    }
    
    // 인덱스 병합을 위한 헬퍼 메소드
    private void MergeIndices(Dictionary<string, HashSet<Type>> target, Dictionary<string, HashSet<Type>> source)
    {
        foreach (var pair in source)
        {
            if (!target.TryGetValue(pair.Key, out var targetSet))
            {
                target[pair.Key] = new HashSet<Type>(pair.Value);
            }
            else
            {
                targetSet.UnionWith(pair.Value);
            }
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
        
        // 4. 약어 매칭 - 모든 검색어에 대해 시도 (대문자 검색어만 제한하지 않음)
        if (_acronymToTypesIndex.TryGetValue(term.ToLowerInvariant(), out var acronymMatches))
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

        // 제네릭 타입 처리 - 제네릭 파라미터 부분 제거
        var genericIndex = input.IndexOf('`');
        if (genericIndex > 0)
            input = input.Substring(0, genericIndex);

        var currentWord = new StringBuilder(input[0].ToString());
        bool wasUpper = char.IsUpper(input[0]);
        bool wasDigit = char.IsDigit(input[0]);
        
        for (int i = 1; i < input.Length; i++)
        {
            var c = input[i];
            bool isUpper = char.IsUpper(c);
            bool isDigit = char.IsDigit(c);
            bool isSpecial = !char.IsLetterOrDigit(c);
            
            // 특수문자는 단어 구분자로 처리
            if (isSpecial)
            {
                if (currentWord.Length > 0)
                {
                    yield return currentWord.ToString();
                    currentWord.Clear();
                }
                continue;
            }
            
            // 1. 소문자 -> 대문자 전환점: 새 단어 시작
            // 2. 숫자 -> 문자 전환점: 새 단어 시작
            // 3. 연속된 대문자 다음에 소문자 (약어 다음에 새 단어): 약어 분리 후 새 단어 시작
            // 4. 연속된 숫자 다음에 문자: 새 단어 시작
            
            bool isTransition = 
                (wasUpper && !isUpper) || // 대문자 → 소문자
                (!wasUpper && isUpper) || // 소문자 → 대문자
                (wasDigit != isDigit);    // 숫자 ↔ 문자 전환점
                
            // 약어 처리 (연속된 대문자 다음에 소문자가 오면 마지막 대문자는 다음 단어의 시작)
            bool isAcronymTransition = wasUpper && isUpper && 
                (i + 1 < input.Length && !char.IsUpper(input[i + 1]) && !char.IsDigit(input[i + 1]));
                
            if (isTransition || isAcronymTransition)
            {
                if (currentWord.Length > 0)
                {
                    // 약어 전환점이면 마지막 문자를 현재 단어에서 제외
                    if (isAcronymTransition && currentWord.Length > 1)
                    {
                        var word = currentWord.ToString();
                        yield return word;
                        currentWord.Clear();
                    }
                    else 
                    {
                        yield return currentWord.ToString();
                        currentWord.Clear();
                    }
                }
            }
            
            currentWord.Append(c);
            wasUpper = isUpper;
            wasDigit = isDigit;
        }
        
        if (currentWord.Length > 0)
            yield return currentWord.ToString();
    }

    /// <summary>
    /// 최소한의 기능으로 백업 초기화를 수행합니다.
    /// </summary>
    private void BackupInitialize()
    {
        try
        {
            var allTypes = new List<Type>();
            var rootNodes = new List<NamespaceTypeNode>();
            
            // 기본 시스템 어셈블리 제외
            var systemAssemblies = new HashSet<string> {
                "mscorlib", "System", "System.Core", "WindowsBase", "PresentationCore", "PresentationFramework"
            };
            
            // 안전하게 접근 가능한 어셈블리만 처리
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a != null && !systemAssemblies.Contains(a.GetName().Name))
                .ToList();
                
            foreach (var assembly in assemblies)
            {
                try
                {
                    Type[] types = new Type[0];
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        if (ex.Types != null)
                        {
                            types = ex.Types.Where(t => t != null).ToArray();
                        }
                    }
                    
                    // 안전하게 타입 추가
                    if (types.Length > 0)
                    {
                        allTypes.AddRange(types);
                    }
                }
                catch
                {
                    // 개별 어셈블리 예외는 무시
                }
            }
            
            // 중복 제거 및 정렬
            _allTypes = allTypes.Distinct().OrderBy(t => t.FullName).ToList();
            _pluginTypes = new List<Type>(); // 플러그인 타입은 빈 리스트로 초기화
            
            // 최소한의 검색 인덱스 구축
            var wordIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
            var namespaceIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var type in _allTypes)
            {
                try
                {
                    // 타입 이름의 단어 추출 (간소화된 방식)
                    var typeName = type.Name;
                    
                    AddToIndex(wordIndex, typeName.ToLowerInvariant(), type);
                    
                    // 네임스페이스 인덱싱 (간소화된 방식)
                    if (!string.IsNullOrEmpty(type.Namespace))
                    {
                        AddToIndex(namespaceIndex, type.Namespace.ToLowerInvariant(), type);
                    }
                }
                catch
                {
                    // 개별 타입 처리 중 예외는 무시
                }
            }
            
            // 최소한의 인덱스 설정
            _wordToTypesIndex = wordIndex;
            _namespaceToTypesIndex = namespaceIndex;
            _prefixToTypesIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
            _acronymToTypesIndex = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
            
            // 네임스페이스 트리 구성 (최소한의 방식)
            try
            {
                _rootNamespaceNodes = CreateNamespaceNodes(_allTypes, CancellationToken.None).ToList();
                _pluginNamespaceNodes = new List<NamespaceTypeNode>(); // 빈 목록으로 초기화
            }
            catch
            {
                // 네임스페이스 트리 구성 실패 시 빈 목록으로 초기화
                _rootNamespaceNodes = new List<NamespaceTypeNode>();
                _pluginNamespaceNodes = new List<NamespaceTypeNode>();
            }
            
            System.Diagnostics.Debug.WriteLine($"백업 초기화 완료: 타입 {_allTypes.Count}개 로드됨");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"백업 초기화 중 오류 발생: {ex.Message}");
            throw; // 백업 초기화도 실패하면 다시 예외 발생
        }
    }
}

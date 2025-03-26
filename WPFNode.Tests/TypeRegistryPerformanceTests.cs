using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using WPFNode.Services;
using WPFNode.ViewModels.Nodes;

namespace WPFNode.Tests
{
    public class TypeRegistryPerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public TypeRegistryPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }
        // 초기화 성능 테스트
        [Fact]
        public async Task TestTypeRegistryInitializationPerformance()
        {
            // TypeRegistry 초기화 상태 리셋을 위한 방법이 있으면 활용
            // 현재는 싱글톤이므로 테스트 간에 완전히 리셋이 어려울 수 있음
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // TypeRegistry 초기화 성능 측정
            await TypeRegistry.Instance.InitializeAsync();
            
            stopwatch.Stop();
            var initTime = stopwatch.ElapsedMilliseconds;
            _output.WriteLine($"TypeRegistry 초기화 시간: {initTime}ms");
            
            // 초기화는 합리적인 시간 내에 완료되어야 함
            Assert.True(initTime < 5000, 
                $"TypeRegistry 초기화가 너무 오래 걸립니다: {initTime}ms");
        }
        
        // 다양한 검색 패턴에 대한 성능 테스트
        [Fact]
        public async Task TestSearchPerformanceWithVariousPatterns()
        {
            // TypeRegistry 미리 초기화
            await TypeRegistry.Instance.InitializeAsync();
            
            // 다양한 검색 시나리오 테스트
            TestSearchPerformance("String");     // 일반적인 타입 이름
            TestSearchPerformance("Int");        // 짧은 이름
            TestSearchPerformance("Dictionary"); // 컬렉션 타입
            TestSearchPerformance("Exception");  // 다수의 결과 예상
            TestSearchPerformance("Employee");   // 프로젝트 특화 타입
            TestSearchPerformance("NonExistentType"); // 결과 없음
            TestSearchPerformance("a");          // 매우 짧은 검색어(많은 결과)
            TestSearchPerformance("System");     // 네임스페이스 검색
        }
        
        private void TestSearchPerformance(string searchTerm)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // 검색 성능 측정
            var results = TypeRegistry.Instance.SearchTypes(searchTerm);
            
            stopwatch.Stop();
            var searchTime = stopwatch.ElapsedMilliseconds;
            _output.WriteLine($"검색 '{searchTerm}' - 시간: {searchTime}ms, 결과 수: {results.Count}");
            
            // 검색은 일반적으로 매우 빠르게 수행되어야 함
            Assert.True(searchTime < 100, 
                $"검색 '{searchTerm}'이 너무 오래 걸립니다: {searchTime}ms");
        }
        
        // 검색 알고리즘 성능 테스트 (다양한 크기의 결과셋)
        [Fact]
        public async Task TestSearchPerformanceWithDifferentResultSizes()
        {
            // TypeRegistry 미리 초기화
            await TypeRegistry.Instance.InitializeAsync();
            
            // 결과 크기에 따른 성능 테스트
            MeasureSearchWithResultSize("", "모든 타입");  // 모든 타입 (최대 결과)
            MeasureSearchWithResultSize("System", "네임스페이스");  // 많은 타입 (중간 결과)
            MeasureSearchWithResultSize("List", "일반 타입");  // 몇 개의 타입 (적은 결과)
            MeasureSearchWithResultSize("Employee", "특정 타입");  // 매우 적은 타입 (최소 결과)
        }
        
        private void MeasureSearchWithResultSize(string searchTerm, string description)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // 검색 성능 측정
            var results = TypeRegistry.Instance.SearchTypes(searchTerm);
            
            stopwatch.Stop();
            var searchTime = stopwatch.ElapsedMilliseconds;
            _output.WriteLine($"{description} 검색 '{searchTerm}' - 시간: {searchTime}ms, 결과 수: {results.Count}");
            
            // 결과 수와 검색 시간의 관계 확인 (결과가 많아도 성능이 크게 저하되지 않아야 함)
            var expectedMaxTime = Math.Min(100 + (results.Count / 100), 500);  // 결과 수에 따른 예상 최대 시간
            Assert.True(searchTime < expectedMaxTime, 
                $"{description} 검색 '{searchTerm}'이 결과 수({results.Count})에 비해 너무 오래 걸립니다: {searchTime}ms");
        }
        
        // 플러그인 전용 검색 성능 테스트
        [Fact]
        public async Task TestPluginOnlySearchPerformance()
        {
            // TypeRegistry 미리 초기화
            await TypeRegistry.Instance.InitializeAsync();
            
            // 일반 검색과 플러그인 전용 검색 비교
            CompareRegularAndPluginSearch("Node");
            CompareRegularAndPluginSearch("Employee");
            CompareRegularAndPluginSearch("String");
        }
        
        private void CompareRegularAndPluginSearch(string searchTerm)
        {
            // 일반 검색 성능 측정
            var stopwatchRegular = new Stopwatch();
            stopwatchRegular.Start();
            var regularResults = TypeRegistry.Instance.SearchTypes(searchTerm, false);
            stopwatchRegular.Stop();
            var regularTime = stopwatchRegular.ElapsedMilliseconds;
            
            // 플러그인 전용 검색 성능 측정
            var stopwatchPlugin = new Stopwatch();
            stopwatchPlugin.Start();
            var pluginResults = TypeRegistry.Instance.SearchTypes(searchTerm, true);
            stopwatchPlugin.Stop();
            var pluginTime = stopwatchPlugin.ElapsedMilliseconds;
            
            _output.WriteLine($"검색 '{searchTerm}' - 일반: {regularTime}ms ({regularResults.Count}개 결과), " +
                              $"플러그인 전용: {pluginTime}ms ({pluginResults.Count}개 결과)");
            
            // 플러그인 전용 검색이 일반 검색보다 빨라야 함 (타입 수가 적으므로)
            Assert.True(pluginTime <= regularTime, 
                $"플러그인 전용 검색({pluginTime}ms)이 일반 검색({regularTime}ms)보다 빠르지 않습니다.");
        }
        
        // 기존 TreeView와 새로운 ListView 방식의 UI 처리 성능 비교 테스트
        // (실제 UI는 테스트에서 생성할 수 없으므로 핵심 로직만 비교)
        [Fact]
        public async Task TestTreeViewVsListViewLogicPerformance()
        {
            // TypeRegistry 미리 초기화
            await TypeRegistry.Instance.InitializeAsync();
            
            // 검색어 지정
            string searchTerm = "Dictionary";
            
            // TreeView 방식 성능 측정 (SetFilteredTypes, HasMatchedTypes 호출 등 UI 없이 핵심 로직만)
            var stopwatchTree = new Stopwatch();
            stopwatchTree.Start();
            
            // 검색 결과 가져오기
            var searchResults = TypeRegistry.Instance.SearchTypes(searchTerm);
            
            // 네임스페이스 노드 트리 가져오기
            var namespaceNodes = TypeRegistry.Instance.GetNamespaceNodes().ToList();
            
            // HashSet 변환 (SetFilteredTypes 사용을 위해)
            var typesSet = new HashSet<Type>(searchResults);
            
            // 필터링 수행 - 이 부분이 TreeView 방식의 핵심 병목점
            foreach (var node in namespaceNodes)
            {
                node.SetFilteredTypes(typesSet);
            }
            
            // 필터링된 노드만 선택
            var filteredNodes = namespaceNodes.Where(n => n.HasMatchedTypes()).ToList();
            
            stopwatchTree.Stop();
            var treeTime = stopwatchTree.ElapsedMilliseconds;
            
            // ListView 방식 성능 측정 (단순 정렬 및 리스트 바인딩)
            var stopwatchList = new Stopwatch();
            stopwatchList.Start();
            
            // 검색 결과 다시 가져오기 (이전 결과를 재사용하지 않음)
            var searchResultsForList = TypeRegistry.Instance.SearchTypes(searchTerm);
            
            // 결과 정렬 - ListView 방식의 핵심 로직
            var orderedResults = searchResultsForList.OrderBy(t => t.Name).ToList();
            
            stopwatchList.Stop();
            var listTime = stopwatchList.ElapsedMilliseconds;
            
            _output.WriteLine($"TreeView 방식 처리 시간: {treeTime}ms (필터링된 노드: {filteredNodes.Count})");
            _output.WriteLine($"ListView 방식 처리 시간: {listTime}ms (정렬된 결과: {orderedResults.Count})");
            
            // ListView 방식이 TreeView 방식보다 빨라야 함
            Assert.True(listTime < treeTime, 
                $"ListView 방식({listTime}ms)이 TreeView 방식({treeTime}ms)보다 빠르지 않습니다.");
        }
    }
}

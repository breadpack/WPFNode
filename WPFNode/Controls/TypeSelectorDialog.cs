using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using WPFNode.Interfaces;
using WPFNode.Services;
using WPFNode.ViewModels.Nodes;

namespace WPFNode.Controls;

public class TypeSelectorDialog : Window
{
    private readonly bool _pluginOnly;
    private readonly TextBox _searchBox;
    private readonly TreeView _namespaceTree;
    private Type? _selectedType;
    private CancellationTokenSource _searchCts = new();
    private readonly DispatcherTimer _searchDebounceTimer;
    
    // 로딩 인디케이터
    private readonly ProgressBar _loadingIndicator;
    private readonly Grid _mainGrid;

    public Type? SelectedType => _selectedType;

    public TypeSelectorDialog(bool pluginOnly = false)
    {
        _pluginOnly = pluginOnly;
        Title = "타입 선택";
        Width = 600;
        Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;
        SizeToContent = SizeToContent.Manual;

        // 검색 딜레이 타이머
        _searchDebounceTimer = new DispatcherTimer {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _searchDebounceTimer.Tick += OnSearchTimerTick;

        _mainGrid = new Grid();
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // 검색 박스
        _searchBox = new TextBox
        {
            Margin = new Thickness(5),
            Height = 23,
            IsEnabled = true // 초기화 완료 상태로 시작
        };
        _searchBox.Text = "검색어를 입력하세요...";  // PlaceholderText 대신 일반 Text 사용
        _searchBox.GotFocus += (s, e) => 
        {
            if (_searchBox.Text == "검색어를 입력하세요...")
                _searchBox.Text = "";
        };
        _searchBox.LostFocus += (s, e) => 
        {
            if (string.IsNullOrWhiteSpace(_searchBox.Text))
                _searchBox.Text = "검색어를 입력하세요...";
        };
        _searchBox.TextChanged += OnSearchTextChanged;
        _mainGrid.Children.Add(_searchBox);
        Grid.SetRow(_searchBox, 0);

        // 네임스페이스 트리
        _namespaceTree = new TreeView
        {
            Margin = new Thickness(5)
        };
        ScrollViewer.SetCanContentScroll(_namespaceTree, true);
        VirtualizingPanel.SetIsVirtualizing(_namespaceTree, true);
        VirtualizingPanel.SetVirtualizationMode(_namespaceTree, VirtualizationMode.Recycling);

        // TreeView 데이터 템플릿 설정
        var typeTemplate = new DataTemplate(typeof(Type));
        var typeTextBlock = new FrameworkElementFactory(typeof(TextBlock));
        typeTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        typeTemplate.VisualTree = typeTextBlock;

        var namespaceTemplate = new HierarchicalDataTemplate(typeof(NamespaceTypeNode));
        var namespaceTextBlock = new FrameworkElementFactory(typeof(TextBlock));
        namespaceTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        namespaceTextBlock.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        namespaceTemplate.VisualTree = namespaceTextBlock;
        
        // Children (하위 네임스페이스)에 대한 바인딩
        namespaceTemplate.ItemsSource = new Binding("Children");
        
        // Types에 대한 바인딩을 위한 컬렉션을 추가
        var typesTemplate = new HierarchicalDataTemplate(typeof(Type));
        var typesTextBlock = new FrameworkElementFactory(typeof(TextBlock));
        typesTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        typesTemplate.VisualTree = typesTextBlock;

        _namespaceTree.Resources.Add(typeof(Type), typesTemplate);
        _namespaceTree.Resources.Add(typeof(NamespaceTypeNode), namespaceTemplate);
        
        _namespaceTree.ItemTemplate = namespaceTemplate;

        _namespaceTree.SelectedItemChanged += OnTreeViewSelectedItemChanged;
        _mainGrid.Children.Add(_namespaceTree);
        Grid.SetRow(_namespaceTree, 1);

        // 버튼 패널
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(5)
        };

        var okButton = new Button
        {
            Content = "확인",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5),
            IsDefault = true
        };
        okButton.Click += (s, e) => 
        {
            DialogResult = true;
            Close();
        };

        var cancelButton = new Button
        {
            Content = "취소",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5),
            IsCancel = true
        };
        cancelButton.Click += (s, e) => Close();

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        _mainGrid.Children.Add(buttonPanel);
        Grid.SetRow(buttonPanel, 2);

        // 로딩 인디케이터
        _loadingIndicator = new ProgressBar
        {
            IsIndeterminate = true,
            Height = 5,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(5, 0, 5, 0),
            Visibility = Visibility.Visible // 초기화 시작 시 표시
        };
        _mainGrid.Children.Add(_loadingIndicator);
        Grid.SetRow(_loadingIndicator, 0);

        Content = _mainGrid;

        // UI가 준비되면 타입 초기화 시작
        try
        {
            // TypeRegistry 동기식 초기화 - GetAwaiter().GetResult()를 사용하여 동기적으로 기다림
            TypeRegistry.Instance.InitializeAsync().GetAwaiter().GetResult();
            
            // 초기화 완료 후 로딩 표시 숨김
            _loadingIndicator.Visibility = Visibility.Collapsed;
            
            // 네임스페이스 트리 초기화
            InitializeNamespaceTree();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"타입 정보를 로드하는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            Close(); // 초기화 실패 시 다이얼로그 닫기
        }

        Loaded += OnDialogLoaded;
        Closing += (s, e) => _searchCts.Cancel();
    }

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        // 생성자에서 이미 초기화가 완료되었으므로 포커스만 설정
        _searchBox.Focus();
    }

    // 네임스페이스 노드 캐시를 저장하기 위한 필드 추가
    private List<NamespaceTypeNode>? _allNodes;

    private void InitializeNamespaceTree()
    {
        try
        {
            // TypeRegistry에서 네임스페이스 트리 가져오기
            var nodes = _pluginOnly 
                ? TypeRegistry.Instance.GetPluginNamespaceNodes()
                : TypeRegistry.Instance.GetNamespaceNodes();
            
            // 노드 캐시 저장
            _allNodes = nodes.ToList();
            
            // 네임스페이스 트리 구조 구축
            var namespaceDict = new Dictionary<string, NamespaceTypeNode>();
            
            // 1단계: 모든 노드를 사전에 등록
            foreach (var node in _allNodes)
            {
                namespaceDict[node.FullNamespace] = node;
            }
            
            // 2단계: 자식-부모 관계 설정
            foreach (var node in _allNodes)
            {
                var parentNamespace = GetParentNamespace(node.FullNamespace);
                if (!string.IsNullOrEmpty(parentNamespace) && namespaceDict.TryGetValue(parentNamespace, out var parentNode))
                {
                    // 부모-자식 관계 설정
                    parentNode.AddChild(node);
                }
            }
            
            // 3단계: 최상위 노드만 필터링 (부모가 없는 노드)
            var rootNodes = _allNodes
                .Where(n => !n.FullNamespace.Contains('.') || GetParentNamespace(n.FullNamespace) == null)
                .ToList();
            
            // UI 트리뷰에 최상위 노드 바인딩 - 자식 노드들은 이미 설정됨
            _namespaceTree.ItemsSource = rootNodes;
            
            // 모든 노드 UI 업데이트 확인
            foreach (var node in rootNodes)
            {
                EnsureNodeInitialized(node);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"네임스페이스 트리 초기화 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    // 노드와 그 하위 노드가 모두 초기화되었는지 확인
    private void EnsureNodeInitialized(NamespaceTypeNode node)
    {
        // IsExpanded 속성 확인 및 설정 - 트리뷰 아이템 초기화 강제
        if (node.Children.Count > 0)
        {
            // 노드의 타입을 처리 (SetFilteredTypes는 내부적으로 UI 업데이트 수행)
            // null을 전달하면 모든 타입을 표시
            node.SetFilteredTypes(null);
            
            // 초기 상태에서는 최상위 노드를 시각적으로 표시
            if (node.FullNamespace.Split('.').Length <= 2)
            {
                // 처음 2단계 깊이까지는 확장
                node.IsExpanded = true;
            }
            else if (string.IsNullOrWhiteSpace(_searchBox.Text) || _searchBox.Text == "검색어를 입력하세요...")
            {
                // 더 깊은 레벨은 접기
                node.IsExpanded = false;
            }
            
            // 자식 노드들도 재귀적으로 초기화
            foreach (var child in node.Children.OfType<NamespaceTypeNode>())
            {
                EnsureNodeInitialized(child);
            }
        }
    }
    
    // 이 메소드는 초기화 로직 개선으로 제거됨 (InitializeNamespaceTree에서 직접 처리)
    
    // 부모 네임스페이스 이름 가져오기
    private string? GetParentNamespace(string fullNamespace)
    {
        var lastDotIndex = fullNamespace.LastIndexOf('.');
        return lastDotIndex > 0 ? fullNamespace.Substring(0, lastDotIndex) : null;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        // 타이머 재설정 (디바운싱)
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private void OnSearchTimerTick(object? sender, EventArgs e)
    {
        _searchDebounceTimer.Stop();
        
        // 이전 검색 취소 및 리소스 정리
        if (_searchCts != null)
        {
            _searchCts.Cancel();
            _searchCts.Dispose();
        }
        
        // 새 검색 CancellationTokenSource 생성 및 할당
        _searchCts = new CancellationTokenSource();
        
        // 새 토큰으로 검색 수행
        PerformSearchAsync(_searchBox.Text, _searchCts.Token);
    }

    private void PerformSearchAsync(string searchText, CancellationToken token)
    {
        if (searchText == "검색어를 입력하세요..." || string.IsNullOrWhiteSpace(searchText))
        {
            // 검색이 없을 때는 전체 트리 표시
            InitializeNamespaceTree();
            return;
        }

        try
        {
            // 동기식으로 직접 검색 수행
            var matchedTypes = TypeRegistry.Instance.SearchTypes(searchText, _pluginOnly);
            
            if (token.IsCancellationRequested) return;
            
            // UI 스레드에서 트리 즉시 업데이트
            UpdateTreeWithSearchResults(matchedTypes);
        }
        catch (OperationCanceledException)
        {
            // 검색 취소됨 - 무시
        }
        catch (Exception ex)
        {
            MessageBox.Show($"검색 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateTreeWithSearchResults(List<Type> matchedTypes)
    {
        try
        {
            // 이미 완전히 초기화된 트리가 없으면 초기화
            if (_allNodes == null || _allNodes.Count == 0)
            {
                InitializeNamespaceTree();
                
                // 검색 결과가 없으면 종료
                if (matchedTypes.Count == 0)
                {
                    return;
                }
            }
            
            // 네임스페이스 트리에서 루트 노드 가져오기
            var rootNodes = _allNodes!.Where(n => !n.FullNamespace.Contains('.')).ToList();
            
            // 검색 결과가 없는 경우 전체 트리 표시
            if (matchedTypes.Count == 0)
            {
                _namespaceTree.ItemsSource = rootNodes;
                return;
            }
            
            // 타입 집합 생성 - 성능 최적화를 위해 HashSet 사용
            var typesSet = new HashSet<Type>(matchedTypes);
            
            // 모든 루트 노드에 대해 필터 적용
            foreach (var node in rootNodes)
            {
                // SetFilteredTypes는 재귀적으로 모든 자식 노드에 필터 적용
                node.SetFilteredTypes(typesSet);
            }
            
            // 검색 결과가 있는 루트 노드만 표시
            var filteredRootNodes = rootNodes
                .Where(n => n.HasMatchedTypes())
                .OrderBy(n => n.Name)
                .ToList();
            
            // UI에 바인딩
            _namespaceTree.ItemsSource = filteredRootNodes;
            
            // 모든 매치된 노드 자동 확장 및 깊은 레벨의 노드도 확장
            foreach (var node in filteredRootNodes)
            {
                ExpandMatchedNodes(node, typesSet);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"검색 결과 업데이트 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    // 매치된 노드와 그 부모 노드들을 재귀적으로 확장
    private void ExpandMatchedNodes(NamespaceTypeNode node, HashSet<Type> matchedTypes)
    {
        // 현재 노드가 매치된 타입을 포함하거나 매치된 자식 노드를 포함하면 확장
        if (node.HasMatchedTypes())
        {
            node.IsExpanded = true;
            
            // 자식 노드들도 재귀적으로 확인
            foreach (var child in node.Children.OfType<NamespaceTypeNode>())
            {
                ExpandMatchedNodes(child, matchedTypes);
            }
        }
    }

    private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is Type type)
        {
            _selectedType = type;
        }
    }
}

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
    private readonly ListView _resultListView; // 검색 결과 표시용 ListView 추가
    private Type? _selectedType;
    private CancellationTokenSource? _searchCts;
    private readonly System.Timers.Timer _searchDebounceTimer;
    
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

        // System.Timers.Timer로 변경
        _searchDebounceTimer = new System.Timers.Timer(300);
        _searchDebounceTimer.Elapsed += OnSearchTimerElapsed;
        _searchDebounceTimer.AutoReset = false; // 한 번만 실행
        _searchDebounceTimer.Start();

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
        
        // 검색 결과 리스트뷰
        _resultListView = new ListView
        {
            Margin = new Thickness(5),
            Visibility = Visibility.Collapsed // 초기에는 숨김 상태
        };
        ScrollViewer.SetCanContentScroll(_resultListView, true);
        VirtualizingPanel.SetIsVirtualizing(_resultListView, true);
        VirtualizingPanel.SetVirtualizationMode(_resultListView, VirtualizationMode.Recycling);
        
        // 리스트뷰 타입 템플릿 설정
        var resultTemplate = new DataTemplate(typeof(Type));
        var typeStackPanel = new FrameworkElementFactory(typeof(StackPanel));
        typeStackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        
        var nameTextBlock = new FrameworkElementFactory(typeof(TextBlock));
        nameTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Name") { Mode = BindingMode.OneTime });
        nameTextBlock.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        
        var namespaceTextBlock = new FrameworkElementFactory(typeof(TextBlock));
        namespaceTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Namespace") { Mode = BindingMode.OneTime });
        namespaceTextBlock.SetValue(TextBlock.FontSizeProperty, 11.0);
        namespaceTextBlock.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.Gray));
        
        typeStackPanel.AppendChild(nameTextBlock);
        typeStackPanel.AppendChild(namespaceTextBlock);
        
        resultTemplate.VisualTree = typeStackPanel;
        _resultListView.ItemTemplate = resultTemplate;
        
        // 리스트뷰 선택 이벤트 연결
        _resultListView.SelectionChanged += (s, e) => {
            if (_resultListView.SelectedItem is Type type)
            {
                _selectedType = type;
            }
        };
        
        // 더블 클릭으로 바로 선택 완료
        _resultListView.MouseDoubleClick += (s, e) => {
            if (_selectedType != null)
            {
                DialogResult = true;
                Close();
            }
        };

        // TreeView 데이터 템플릿 설정
        var treeTypeTemplate = new DataTemplate(typeof(Type));
        var treeTypeTextBlock = new FrameworkElementFactory(typeof(TextBlock));
        treeTypeTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        treeTypeTemplate.VisualTree = treeTypeTextBlock;

        var namespaceTemplate = new HierarchicalDataTemplate(typeof(NamespaceTypeNode));
        var treeNamespaceTextBlock = new FrameworkElementFactory(typeof(TextBlock));
        treeNamespaceTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        treeNamespaceTextBlock.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        namespaceTemplate.VisualTree = treeNamespaceTextBlock;
        
        // Children (하위 네임스페이스)에 대한 바인딩
        namespaceTemplate.ItemsSource = new Binding("Children");
        
        // Types에 대한 바인딩을 위한 컬렉션을 추가
        var typesTemplate = new HierarchicalDataTemplate(typeof(Type));
        var typesTextBlock = new FrameworkElementFactory(typeof(TextBlock));
        typesTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        typesTemplate.VisualTree = typesTextBlock;

        _namespaceTree.Resources.Add(typeof(Type), treeTypeTemplate);
        _namespaceTree.Resources.Add(typeof(NamespaceTypeNode), namespaceTemplate);
        
        _namespaceTree.ItemTemplate = namespaceTemplate;

        _namespaceTree.SelectedItemChanged += OnTreeViewSelectedItemChanged;
        _mainGrid.Children.Add(_namespaceTree);
        Grid.SetRow(_namespaceTree, 1);
        
        // 리스트뷰를 그리드에 추가
        _mainGrid.Children.Add(_resultListView);
        Grid.SetRow(_resultListView, 1);

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
        Loaded += OnDialogLoaded;
        Closing += OnDialogClosing;

        // 비동기 초기화 시작
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            // TypeRegistry 비동기 초기화
            await TypeRegistry.Instance.InitializeAsync();
            
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
    }

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        // 포커스 설정
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
        }
        catch (Exception ex)
        {
            MessageBox.Show($"네임스페이스 트리 초기화 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    // 부모 네임스페이스 이름 가져오기
    private string? GetParentNamespace(string fullNamespace)
    {
        var lastDotIndex = fullNamespace.LastIndexOf('.');
        return lastDotIndex > 0 ? fullNamespace.Substring(0, lastDotIndex) : null;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        // 타이머 재설정
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private void OnSearchTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // UI 스레드에서 검색 수행
        Application.Current.Dispatcher.Invoke(() => {
            try
            {
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
            catch (ObjectDisposedException)
            {
                // 이미 해제된 경우 무시
            }
        });
    }

    private async void PerformSearchAsync(string searchText, CancellationToken token)
    {
        try
        {
            // 로딩 표시 활성화
            _loadingIndicator.Visibility = Visibility.Visible;
            
            if (searchText == "검색어를 입력하세요..." || string.IsNullOrWhiteSpace(searchText))
            {
                // 검색이 없을 때는 TreeView만 표시
                _namespaceTree.Visibility = Visibility.Visible;
                _resultListView.Visibility = Visibility.Collapsed;
                
                // 필터 제거 (모든 노드 표시)
                if (_allNodes != null && _allNodes.Count > 0)
                {
                    // 루트 노드 찾기 - UI 스레드에서 바로 업데이트
                    var rootNodes = _allNodes
                        .Where(n => !n.FullNamespace.Contains('.') || GetParentNamespace(n.FullNamespace) == null)
                        .ToList();
                    
                    // UI에 바인딩
                    _namespaceTree.ItemsSource = rootNodes;
                }
                
                // 로딩 표시 비활성화
                _loadingIndicator.Visibility = Visibility.Collapsed;
                return;
            }

            // 백그라운드 스레드에서 검색 수행
            var matchedTypes = await Task.Run(() => {
                // 검색 수행
                return TypeRegistry.Instance.SearchTypes(searchText, _pluginOnly);
            }, token);
            
            if (token.IsCancellationRequested) return;

            // 검색 결과가 있으면 ListView로 표시 (TreeView 필터링 제거)
            if (matchedTypes.Count > 0)
            {
                // TreeView 숨기고 ListView만 표시
                _namespaceTree.Visibility = Visibility.Collapsed;
                _resultListView.Visibility = Visibility.Visible;
                
                // ListView에 바로 결과 바인딩 (정렬만 수행)
                _resultListView.ItemsSource = matchedTypes.OrderBy(t => t.Name);
            }
            else
            {
                // 결과가 없을 때는 TreeView 표시 (전체 트리)
                _namespaceTree.Visibility = Visibility.Visible;
                _resultListView.Visibility = Visibility.Collapsed;
                
                // 결과 없음을 표시하는 메시지를 보여줄 수도 있음
                MessageBox.Show($"'{searchText}' 검색 결과가 없습니다.", "검색 결과", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // TreeView 초기 상태로 복원
                if (_allNodes != null && _allNodes.Count > 0)
                {
                    // 루트 노드 찾기
                    var rootNodes = _allNodes
                        .Where(n => !n.FullNamespace.Contains('.') || GetParentNamespace(n.FullNamespace) == null)
                        .ToList();
                    
                    // UI에 바인딩
                    _namespaceTree.ItemsSource = rootNodes;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 검색 취소됨 - 무시
        }
        catch (Exception ex)
        {
            MessageBox.Show($"검색 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // 로딩 표시 비활성화
            _loadingIndicator.Visibility = Visibility.Collapsed;
        }
    }

    private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is Type type)
        {
            _selectedType = type;
        }
    }

    private void OnDialogClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            _searchCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // 이미 해제된 경우 무시
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            _searchDebounceTimer?.Dispose();
            _searchCts?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // 이미 해제된 경우 무시
        }
        finally
        {
            base.OnClosing(e);
        }
    }
}

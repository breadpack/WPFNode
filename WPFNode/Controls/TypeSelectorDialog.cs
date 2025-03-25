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
    private readonly CancellationTokenSource _searchCts = new();
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
            IsEnabled = false // 초기에는 비활성화 (타입 로딩 때문에)
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
            Margin = new Thickness(5, 0, 5, 0)
        };
        _mainGrid.Children.Add(_loadingIndicator);
        Grid.SetRow(_loadingIndicator, 0);

        Content = _mainGrid;

        Loaded += OnDialogLoaded;
        Closing += (s, e) => _searchCts.Cancel();
    }

    private async void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // TypeRegistry 초기화
            await TypeRegistry.Instance.InitializeAsync();
            
            // UI 업데이트
            _loadingIndicator.Visibility = Visibility.Collapsed;
            _searchBox.IsEnabled = true;

            // 네임스페이스 트리 초기화
            InitializeNamespaceTree();
            _searchBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"타입 정보를 로드하는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InitializeNamespaceTree()
    {
        try
        {
            // TypeRegistry에서 네임스페이스 트리 가져오기
            var nodes = _pluginOnly 
                ? TypeRegistry.Instance.GetPluginNamespaceNodes()
                : TypeRegistry.Instance.GetNamespaceNodes();
                
            _namespaceTree.ItemsSource = nodes;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"네임스페이스 트리 초기화 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
        
        // 이전 검색 취소
        _searchCts.Cancel();
        
        // 새 검색 시작
        var newCts = new CancellationTokenSource();
        var token = newCts.Token;
        
        PerformSearchAsync(_searchBox.Text, token);
    }

    private async void PerformSearchAsync(string searchText, CancellationToken token)
    {
        if (searchText == "검색어를 입력하세요..." || string.IsNullOrWhiteSpace(searchText))
        {
            // 검색이 없을 때는 전체 트리 표시
            InitializeNamespaceTree();
            return;
        }

        try
        {
            // TypeRegistry의 최적화된 검색 사용
            var matchedTypes = await Task.Run(() => 
                TypeRegistry.Instance.SearchTypes(searchText, _pluginOnly), token);
                
            if (token.IsCancellationRequested) return;
            
            // UI 스레드에서 트리 업데이트
            await Dispatcher.InvokeAsync(() => {
                UpdateTreeWithSearchResults(matchedTypes);
            });
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
            // TypeRegistry에서 네임스페이스 트리 가져오기
            var nodes = _pluginOnly 
                ? TypeRegistry.Instance.GetPluginNamespaceNodes()
                : TypeRegistry.Instance.GetNamespaceNodes();
                
            // 검색 결과로 트리 업데이트
            foreach (var node in nodes)
            {
                node.SetFilteredTypes(matchedTypes);
            }
            
            // 매칭된 타입이 있는 노드만 표시
            var filteredNodes = nodes
                .Where(n => n.HasMatchedTypes())
                .OrderBy(n => n.Name);
                
            _namespaceTree.ItemsSource = filteredNodes;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"검색 결과 업데이트 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
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

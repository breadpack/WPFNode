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
using WPFNode.Extensions;
using WPFNode.Converters;

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
    
    // 제네릭 타입 선택 관련
    private bool _isGenericSelectionMode = false;
    private Type? _baseGenericType;
    private List<Type> _selectedTypeArguments = new List<Type>();
    private int _currentTypeParameterIndex = 0;
    private Label _genericSelectionInfoLabel;
    private Button _backButton;
    private StackPanel _breadcrumbPanel;
    
    // 단계별 선택 표시를 위한 패널
    private StackPanel _selectionStepsPanel;
    private Grid _selectionInfoContainer;

    private readonly Button _nextButton;
    private readonly Button _prevButton;
    private readonly Button _selectGenericArgButton;
    private readonly StackPanel _genericArgPanel;

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
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40, GridUnitType.Pixel) }); // 선택 정보 영역 (고정 높이)
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 검색 박스
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 콘텐츠 영역
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 버튼 영역
        
        // 선택 정보 컨테이너 (고정 높이를 가진 영역으로 항상 표시)
        _selectionInfoContainer = new Grid
        {
            Margin = new Thickness(5),
            Height = 30
        };
        _mainGrid.Children.Add(_selectionInfoContainer);
        Grid.SetRow(_selectionInfoContainer, 0);
        
        // 브레드크럼 패널 (제네릭 타입 선택 경로 표시)
        _breadcrumbPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed // 초기에는 숨겨둠
        };
        _selectionInfoContainer.Children.Add(_breadcrumbPanel);
        
        // 단계별 선택 표시를 위한 패널
        _selectionStepsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Visible
        };
        _selectionStepsPanel.Children.Add(new TextBlock 
        { 
            Text = "Type 선택",
            FontWeight = FontWeights.Bold 
        });
        _selectionInfoContainer.Children.Add(_selectionStepsPanel);
        
        // 제네릭 타입 선택 정보 라벨
        _genericSelectionInfoLabel = new Label
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.Bold,
            Visibility = Visibility.Collapsed // 초기에는 숨겨둠
        };
        _selectionInfoContainer.Children.Add(_genericSelectionInfoLabel);

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
        Grid.SetRow(_searchBox, 1);

        // 네임스페이스 트리
        _namespaceTree = new TreeView
        {
            Margin = new Thickness(5)
        };
        ScrollViewer.SetCanContentScroll(_namespaceTree, true);
        VirtualizingPanel.SetIsVirtualizing(_namespaceTree, true);
        VirtualizingPanel.SetVirtualizationMode(_namespaceTree, VirtualizationMode.Recycling);
        _mainGrid.Children.Add(_namespaceTree);
        Grid.SetRow(_namespaceTree, 2);
        
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
        nameTextBlock.SetBinding(TextBlock.TextProperty, new Binding(".") { 
            Converter = new TypeToUserFriendlyNameConverter(),
            Mode = BindingMode.OneTime 
        });
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
        _resultListView.SelectionChanged += OnListViewSelectionChanged;
        
        // 더블 클릭으로 바로 선택 완료
        _resultListView.MouseDoubleClick += OnListViewDoubleClick;

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
        
        // 리스트뷰를 그리드에 추가
        _mainGrid.Children.Add(_resultListView);
        Grid.SetRow(_resultListView, 2);

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

        // 이전 버튼 (제네릭 타입 선택 모드에서만 활성화)
        _backButton = new Button
        {
            Content = "이전",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5),
            Visibility = Visibility.Collapsed
        };
        _backButton.Click += OnBackButtonClick;
        
        // 제네릭 인자 선택 패널
        _genericArgPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(5),
            Visibility = Visibility.Collapsed
        };

        // 제네릭 인자 선택 버튼
        _selectGenericArgButton = new Button
        {
            Content = "제네릭 인자 선택",
            Width = 120,
            Height = 25,
            Margin = new Thickness(5),
            Visibility = Visibility.Collapsed
        };
        _selectGenericArgButton.Click += OnSelectGenericArgClick;

        // 이전/다음 버튼
        _prevButton = new Button
        {
            Content = "이전",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5),
            Visibility = Visibility.Collapsed
        };
        _prevButton.Click += OnPrevButtonClick;

        _nextButton = new Button
        {
            Content = "다음",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5),
            Visibility = Visibility.Collapsed
        };
        _nextButton.Click += OnNextButtonClick;

        _genericArgPanel.Children.Add(_prevButton);
        _genericArgPanel.Children.Add(_nextButton);
        _genericArgPanel.Children.Add(_selectGenericArgButton);

        // 버튼 패널에 제네릭 인자 선택 패널 추가
        buttonPanel.Children.Insert(0, _genericArgPanel);
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        _mainGrid.Children.Add(buttonPanel);
        Grid.SetRow(buttonPanel, 3);

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
            _loadingIndicator.Visibility = Visibility.Visible;
            
            if (searchText == "검색어를 입력하세요..." || string.IsNullOrWhiteSpace(searchText))
            {
                _namespaceTree.Visibility = Visibility.Visible;
                _resultListView.Visibility = Visibility.Collapsed;
                
                if (_allNodes != null && _allNodes.Count > 0)
                {
                    var rootNodes = _allNodes
                        .Where(n => !n.FullNamespace.Contains('.') || GetParentNamespace(n.FullNamespace) == null)
                        .ToList();
                    
                    _namespaceTree.ItemsSource = rootNodes;
                }
                
                _loadingIndicator.Visibility = Visibility.Collapsed;
                return;
            }

            var matchedTypes = await Task.Run(() => {
                return TypeRegistry.Instance.SearchTypes(searchText, _pluginOnly);
            }, token);
            
            if (token.IsCancellationRequested) return;

            if (matchedTypes.Count > 0)
            {
                _namespaceTree.Visibility = Visibility.Collapsed;
                _resultListView.Visibility = Visibility.Visible;
                
                // 디버그 로깅 추가
                System.Diagnostics.Debug.WriteLine($"TypeSelectorDialog - 검색어: {searchText}");
                foreach (var type in matchedTypes.Take(5))
                {
                    System.Diagnostics.Debug.WriteLine($"TypeSelectorDialog - 타입: {type.FullName}");
                }
                
                _resultListView.ItemsSource = matchedTypes;
            }
            else
            {
                _namespaceTree.Visibility = Visibility.Visible;
                _resultListView.Visibility = Visibility.Collapsed;
                
                MessageBox.Show($"'{searchText}' 검색 결과가 없습니다.", "검색 결과", MessageBoxButton.OK, MessageBoxImage.Information);
                
                if (_allNodes != null && _allNodes.Count > 0)
                {
                    var rootNodes = _allNodes
                        .Where(n => !n.FullNamespace.Contains('.') || GetParentNamespace(n.FullNamespace) == null)
                        .ToList();
                    
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
            _loadingIndicator.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// ListView 선택 이벤트 핸들러
    /// </summary>
    private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_resultListView.SelectedItem is Type type)
        {
            if (_isGenericSelectionMode)
            {
                _selectedTypeArguments[_currentTypeParameterIndex] = type;
                ProcessGenericTypeArgument();
                return;
            }
            
            if (type.IsGenericTypeDefinition)
            {
                _selectedType = type;
                _selectGenericArgButton.Visibility = Visibility.Visible;
                _genericArgPanel.Visibility = Visibility.Visible;
            }
            else
            {
                _selectedType = type;
                _selectGenericArgButton.Visibility = Visibility.Collapsed;
                _genericArgPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
    
    /// <summary>
    /// ListView 더블 클릭 이벤트 핸들러
    /// </summary>
    private void OnListViewDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_selectedType != null && !_isGenericSelectionMode)
        {
            DialogResult = true;
            Close();
        }
    }

    private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is Type type)
        {
            if (_isGenericSelectionMode)
            {
                _selectedTypeArguments[_currentTypeParameterIndex] = type;
                ProcessGenericTypeArgument();
                return;
            }
            
            if (type.IsGenericTypeDefinition)
            {
                _selectedType = type;
                _selectGenericArgButton.Visibility = Visibility.Visible;
                _genericArgPanel.Visibility = Visibility.Visible;
            }
            else
            {
                _selectedType = type;
                _selectGenericArgButton.Visibility = Visibility.Collapsed;
                _genericArgPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
    
    /// <summary>
    /// 제네릭 타입 선택 모드로 전환합니다.
    /// </summary>
    private void EnterGenericSelectionMode(Type genericTypeDefinition)
    {
        _isGenericSelectionMode = true;
        _baseGenericType = genericTypeDefinition;
        _selectedTypeArguments.Clear();
        
        var typeParams = genericTypeDefinition.GetGenericArguments();
        _currentTypeParameterIndex = 0;
        
        for (int i = 0; i < typeParams.Length; i++)
        {
            _selectedTypeArguments.Add(null);
        }
        
        UpdateGenericSelectionUI();
        
        _prevButton.Visibility = Visibility.Visible;
        _nextButton.Visibility = typeParams.Length > 1 ? Visibility.Visible : Visibility.Collapsed;
        _selectGenericArgButton.Visibility = Visibility.Collapsed;
    }
    
    /// <summary>
    /// 제네릭 인자 선택 UI를 업데이트합니다.
    /// </summary>
    private void UpdateGenericSelectionUI()
    {
        if (_baseGenericType == null) return;
        
        var typeParams = _baseGenericType.GetGenericArguments();
        
        var typeParamNames = typeParams.Select(p => p.Name).ToArray();
        var genericName = _baseGenericType.GetUserFriendlyName();
        
        var typeArgDisplay = new List<string>();
        for (int i = 0; i < typeParams.Length; i++)
        {
            if (i < _currentTypeParameterIndex && _selectedTypeArguments[i] != null)
            {
                typeArgDisplay.Add(_selectedTypeArguments[i].GetUserFriendlyName());
            }
            else if (i == _currentTypeParameterIndex)
            {
                typeArgDisplay.Add("?");
            }
            else
            {
                typeArgDisplay.Add("");
            }
        }
        
        // 제네릭 타입 정보와 현재 선택된 타입을 별도의 패널에 표시
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(5)
        };

        // 제네릭 타입 정보
        var genericTypeInfo = new TextBlock
        {
            Text = $"{genericName}<{string.Join(",", typeArgDisplay)}>",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        };
        mainPanel.Children.Add(genericTypeInfo);

        // 현재 선택 중인 타입 정보
        if (_selectedType != null)
        {
            var currentTypeInfo = new TextBlock
            {
                Text = $"현재 선택: {_selectedType.GetUserFriendlyName()}",
                Foreground = Brushes.Blue,
                FontWeight = FontWeights.Bold
            };
            mainPanel.Children.Add(currentTypeInfo);
        }

        _genericSelectionInfoLabel.Content = mainPanel;
        _genericSelectionInfoLabel.Visibility = Visibility.Visible;
        
        UpdateSelectionSteps();
        UpdateBreadcrumb();
        
        var currentParam = typeParams[_currentTypeParameterIndex];
        Title = $"타입 선택 - {currentParam.Name} 타입 인자 선택";
    }
    
    /// <summary>
    /// 단계별 선택 표시를 업데이트합니다 (Origin, T1, T2, ...)
    /// </summary>
    private void UpdateSelectionSteps()
    {
        _selectionStepsPanel.Children.Clear();
        _selectionStepsPanel.Visibility = Visibility.Visible;
        
        if (_baseGenericType == null)
        {
            _selectionStepsPanel.Children.Add(new TextBlock 
            { 
                Text = "Origin 타입 선택", 
                FontWeight = FontWeights.Bold 
            });
            return;
        }
        
        var typeParams = _baseGenericType.GetGenericArguments();
        var genericName = _baseGenericType.Name.Split('`')[0];
        
        // Origin 타입 표시
        _selectionStepsPanel.Children.Add(new TextBlock
        {
            Text = $"Origin: {genericName}",
            Margin = new Thickness(0, 0, 5, 0),
            Foreground = Brushes.Gray
        });
        
        _selectionStepsPanel.Children.Add(new TextBlock 
        { 
            Text = " → ", 
            Margin = new Thickness(0, 0, 5, 0) 
        });
        
        // 제네릭 인자 표시
        for (int i = 0; i < typeParams.Length; i++)
        {
            var brush = (i == _currentTypeParameterIndex) ? Brushes.Blue : 
                       (i < _currentTypeParameterIndex) ? Brushes.Gray : Brushes.Black;
            
            var fontWeight = (i == _currentTypeParameterIndex) ? FontWeights.Bold : FontWeights.Normal;
            
            var argName = $"T{i + 1}";
            if (i < _currentTypeParameterIndex && _selectedTypeArguments[i] != null)
            {
                argName += $": {_selectedTypeArguments[i].Name}";
            }
            else if (i == _currentTypeParameterIndex)
            {
                argName += ": 선택 중...";
            }
            
            _selectionStepsPanel.Children.Add(new TextBlock
            {
                Text = argName,
                Margin = new Thickness(0, 0, 5, 0),
                Foreground = brush,
                FontWeight = fontWeight
            });
            
            if (i < typeParams.Length - 1)
            {
                _selectionStepsPanel.Children.Add(new TextBlock 
                { 
                    Text = " → ", 
                    Margin = new Thickness(0, 0, 5, 0) 
                });
            }
        }
    }
    
    /// <summary>
    /// 브레드크럼 내비게이션을 업데이트합니다.
    /// </summary>
    private void UpdateBreadcrumb()
    {
        _breadcrumbPanel.Children.Clear();
        _breadcrumbPanel.Visibility = Visibility.Visible;
        
        var baseLabel = new TextBlock
        {
            Text = "타입 선택",
            Margin = new Thickness(0, 0, 5, 0)
        };
        _breadcrumbPanel.Children.Add(baseLabel);
        
        _breadcrumbPanel.Children.Add(new TextBlock { Text = " > ", Margin = new Thickness(0, 0, 5, 0) });
        
        if (_baseGenericType != null)
        {
            var genericName = _baseGenericType.Name.Split('`')[0];
            _breadcrumbPanel.Children.Add(new TextBlock 
            { 
                Text = genericName, 
                Margin = new Thickness(0, 0, 5, 0),
                FontWeight = FontWeights.Bold
            });
            
            if (_currentTypeParameterIndex > 0)
            {
                _breadcrumbPanel.Children.Add(new TextBlock { Text = " > ", Margin = new Thickness(0, 0, 5, 0) });
                
                var typeParams = _baseGenericType.GetGenericArguments();
                for (int i = 0; i < _currentTypeParameterIndex; i++)
                {
                    var paramLabel = new TextBlock
                    {
                        Text = $"{typeParams[i].Name}: {(_selectedTypeArguments[i]?.Name ?? "?")}",
                        Margin = new Thickness(0, 0, 5, 0)
                    };
                    _breadcrumbPanel.Children.Add(paramLabel);
                    
                    if (i < _currentTypeParameterIndex - 1)
                    {
                        _breadcrumbPanel.Children.Add(new TextBlock { Text = " > ", Margin = new Thickness(0, 0, 5, 0) });
                    }
                }
            }
            
            // 현재 선택 중인 인자 표시
            if (_selectedType != null)
            {
                _breadcrumbPanel.Children.Add(new TextBlock { Text = " > ", Margin = new Thickness(0, 0, 5, 0) });
                _breadcrumbPanel.Children.Add(new TextBlock
                {
                    Text = $"현재 선택: {_selectedType.Name}",
                    Foreground = Brushes.Blue,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 5, 0)
                });
            }
        }
    }
    
    /// <summary>
    /// 타입 인자 선택 완료 후 다음 단계로 진행합니다.
    /// </summary>
    private void ProcessGenericTypeArgument()
    {
        if (_baseGenericType == null) return;
        
        var typeParams = _baseGenericType.GetGenericArguments();
        
        if (_currentTypeParameterIndex >= typeParams.Length - 1)
        {
            var finalGenericType = CreateGenericType();
            if (finalGenericType != null)
            {
                _selectedType = finalGenericType;
                _isGenericSelectionMode = false;
                _genericSelectionInfoLabel.Visibility = Visibility.Collapsed;
                _prevButton.Visibility = Visibility.Collapsed;
                _nextButton.Visibility = Visibility.Collapsed;
                _selectGenericArgButton.Visibility = Visibility.Collapsed;
                _genericArgPanel.Visibility = Visibility.Collapsed;
                Title = "타입 선택";
            }
        }
        else
        {
            _currentTypeParameterIndex++;
            UpdateGenericSelectionUI();
        }
    }
    
    /// <summary>
    /// 선택한 타입 인자로 최종 제네릭 타입을 생성합니다.
    /// </summary>
    private Type CreateGenericType()
    {
        if (_baseGenericType == null || _selectedTypeArguments.Count == 0)
            return null;
            
        try
        {
            if (_selectedTypeArguments.Any(t => t == null))
                return null;
                
            return _baseGenericType.MakeGenericType(_selectedTypeArguments.ToArray());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"제네릭 타입 생성 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    /// <summary>
    /// 이전 버튼 클릭 이벤트 처리
    /// </summary>
    private void OnBackButtonClick(object sender, RoutedEventArgs e)
    {
        if (!_isGenericSelectionMode)
            return;
            
        if (_currentTypeParameterIndex > 0)
        {
            _currentTypeParameterIndex--;
            UpdateGenericSelectionUI();
        }
        else
        {
            _isGenericSelectionMode = false;
            _genericSelectionInfoLabel.Visibility = Visibility.Collapsed;
            _breadcrumbPanel.Visibility = Visibility.Collapsed;
            _prevButton.Visibility = Visibility.Collapsed;
            _nextButton.Visibility = Visibility.Collapsed;
            _selectGenericArgButton.Visibility = Visibility.Collapsed;
            _genericArgPanel.Visibility = Visibility.Collapsed;
            Title = "타입 선택";
        }
    }

    private void OnSelectGenericArgClick(object sender, RoutedEventArgs e)
    {
        if (_selectedType != null && _selectedType.IsGenericTypeDefinition)
        {
            EnterGenericSelectionMode(_selectedType);
        }
    }

    private void OnPrevButtonClick(object sender, RoutedEventArgs e)
    {
        if (_currentTypeParameterIndex > 0)
        {
            _currentTypeParameterIndex--;
            UpdateGenericSelectionUI();
        }
    }

    private void OnNextButtonClick(object sender, RoutedEventArgs e)
    {
        if (_baseGenericType != null)
        {
            var typeParams = _baseGenericType.GetGenericArguments();
            if (_currentTypeParameterIndex < typeParams.Length - 1)
            {
                _currentTypeParameterIndex++;
                UpdateGenericSelectionUI();
            }
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

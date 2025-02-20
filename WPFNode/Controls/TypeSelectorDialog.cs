using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Reflection;
using WPFNode.Services;
using WPFNode.ViewModels.Nodes;
using System.Windows.Controls.Primitives;
using WPFNode.Interfaces;

namespace WPFNode.Controls;

public class TypeSelectorDialog : Window
{
    private readonly bool _pluginOnly;
    private readonly TextBox _searchBox;
    private readonly TreeView _namespaceTree;
    private Type? _selectedType;
    private IEnumerable<NamespaceTypeNode>? _allNodes;

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

        // 전체 데이터 미리 로드
        LoadAllTypes();

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // 검색 박스
        _searchBox = new TextBox
        {
            Margin = new Thickness(5),
            Height = 23
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
        grid.Children.Add(_searchBox);
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
        grid.Children.Add(_namespaceTree);
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
        grid.Children.Add(buttonPanel);
        Grid.SetRow(buttonPanel, 2);

        Content = grid;

        Loaded += OnDialogLoaded;
    }

    private void OnDialogLoaded(object sender, RoutedEventArgs e)
    {
        InitializeNamespaceTree();
        _searchBox.Focus();
    }

    private void LoadAllTypes()
    {
        var allTypes = _pluginOnly ? 
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
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsInterface);

        // 네임스페이스별로 그룹화하여 노드 생성
        _allNodes = NamespaceTypeNode.CreateRootNodes(_pluginOnly).ToList();
    }

    private void InitializeNamespaceTree()
    {
        // 기존 노드를 그대로 사용
        _namespaceTree.ItemsSource = _allNodes;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_searchBox.Text == "검색어를 입력하세요..." || string.IsNullOrWhiteSpace(_searchBox.Text))
        {
            // 검색이 없을 때는 필터를 제거
            foreach (var node in _allNodes!)
            {
                node.SetFilteredTypes(null);
            }
            InitializeNamespaceTree();
            return;
        }

        var searchText = _searchBox.Text.ToLower();
        var terms = searchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // 검색 조건에 맞는 타입들을 찾음
        var matchedTypes = _allNodes!
            .SelectMany(n => n.Types)
            .Where(t => IsFuzzyMatch(t, terms))
            .ToList();

        // 각 네임스페이스 노드에 필터링된 타입 설정
        foreach (var node in _allNodes!)
        {
            node.SetFilteredTypes(matchedTypes);
        }

        // 매칭된 타입이 있는 노드만 표시
        var filteredNodes = _allNodes!
            .Where(n => n.HasMatchedTypes())
            .Select(n => 
            {
                n.IsExpanded = true;
                return n;
            })
            .OrderBy(n => n.Name);

        _namespaceTree.ItemsSource = filteredNodes;
    }

    private bool IsFuzzyMatch(Type type, string[] searchTerms)
    {
        var typeName = type.Name.ToLower();
        var namespaceName = (type.Namespace ?? "(No Namespace)").ToLower();
        var fullName = $"{namespaceName}.{typeName}";

        return searchTerms.All(term =>
        {
            var termChars = term.ToCharArray();
            var currentPos = 0;

            // 각 검색어의 문자가 순서대로 존재하는지 확인
            foreach (var c in termChars)
            {
                currentPos = fullName.IndexOf(c, currentPos);
                if (currentPos == -1)
                {
                    return false;
                }
                currentPos++;
            }
            return true;
        });
    }

    private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is Type type)
        {
            _selectedType = type;
        }
    }
} 
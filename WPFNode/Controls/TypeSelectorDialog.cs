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
    private IEnumerable<NamespaceTypeNode>? _allNodes;  // 이미 최상위 노드들만 포함

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

        // 네임스페이스별로 그룹화하여 노드 생성 (이미 최상위 노드만 반환)
        _allNodes = CreateNamespaceNodes(allTypes).ToList();
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
            _namespaceTree.ItemsSource = _allNodes;
            return;
        }

        var searchText = _searchBox.Text.ToLower();
        var terms = searchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // 검색 조건에 맞는 타입들을 찾음
        var matchedTypes = _allNodes!
            .SelectMany(n => n.GetAllTypesInHierarchy())
            .Where(t => IsFuzzyMatch(t, terms))
            .ToList();

        // 최상위 네임스페이스 노드들에만 필터링된 타입 설정
        foreach (var node in _allNodes!)
        {
            node.SetFilteredTypes(matchedTypes);
        }

        // 매칭된 타입이 있는 노드만 표시
        var filteredNodes = _allNodes!
            .Where(n => n.HasMatchedTypes())
            .OrderBy(n => n.Name);

        _namespaceTree.ItemsSource = filteredNodes;
    }

    private bool IsFuzzyMatch(Type type, string[] searchTerms)
    {
        var typeName = type.Name;
        var namespaceName = type.Namespace ?? "(No Namespace)";
        
        return searchTerms.All(term =>
        {
            term = term.ToLower();
            
            // 1. 정확한 단어 매칭 (대소문자 구분 없이)
            if (typeName.Equals(term, StringComparison.OrdinalIgnoreCase))
                return true;

            // 2. 타입 이름이 검색어로 시작하는 경우
            if (typeName.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                return true;

            // 3. 파스칼 케이스 단어 매칭
            var words = SplitPascalCase(typeName);
            if (words.Any(w => w.Equals(term, StringComparison.OrdinalIgnoreCase)))
                return true;

            // 4. 약어 매칭 (대문자로만 구성된 검색어의 경우)
            if (term.All(char.IsUpper) && term.Length >= 2)
            {
                var acronym = string.Concat(words.Select(w => w.FirstOrDefault()));
                if (acronym.Contains(term, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // 5. 네임스페이스 매칭 (전체 또는 마지막 부분)
            var nsWords = namespaceName.Split('.');
            var lastNs = nsWords.LastOrDefault() ?? "";
            return lastNs.Equals(term, StringComparison.OrdinalIgnoreCase) ||
                   namespaceName.Equals(term, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static IEnumerable<string> SplitPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            yield break;

        var currentWord = new System.Text.StringBuilder(input[0].ToString());
        
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

    private string? GetParentNamespace(string fullNamespace)
    {
        var lastDotIndex = fullNamespace.LastIndexOf('.');
        return lastDotIndex > 0 ? fullNamespace.Substring(0, lastDotIndex) : null;
    }

    private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is Type type)
        {
            _selectedType = type;
        }
    }

    private IEnumerable<NamespaceTypeNode> CreateNamespaceNodes(IEnumerable<Type> types)
    {
        var namespaceGroups = types.GroupBy(t => t.Namespace ?? "(No Namespace)");
        var nodes = new Dictionary<string, NamespaceTypeNode>();

        foreach (var group in namespaceGroups)
        {
            var ns = group.Key;
            var parts = ns.Split('.');
            var currentNs = "";

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var fullNs = i == 0 ? part : $"{currentNs}.{part}";

                if (!nodes.ContainsKey(fullNs))
                {
                    var node = new NamespaceTypeNode(fullNs, _pluginOnly);
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
} 
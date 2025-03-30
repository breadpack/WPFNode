using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using WPFNode.Interfaces;
using WPFNode.Models;
using NodeCanvasViewModel = WPFNode.ViewModels.Nodes.NodeCanvasViewModel;

namespace WPFNode.Controls;

[ContentProperty(nameof(Content))]
public class SearchPanel : Control {
    private TextBox? _searchBox;
    private Popup?   _popup;
    private ListBox? _resultList;

    public static readonly DependencyProperty PluginServiceProperty =
        DependencyProperty.Register(
            nameof(PluginService),
            typeof(INodeModelService),
            typeof(SearchPanel),
            new PropertyMetadata(null));

    public INodeModelService? PluginService {
        get => (INodeModelService?)GetValue(PluginServiceProperty);
        set => SetValue(PluginServiceProperty, value);
    }

    public SearchPanel() { }

    static SearchPanel() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchPanel),
                                                 new FrameworkPropertyMetadata(typeof(SearchPanel)));
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(NodeCanvasViewModel),
            typeof(SearchPanel),
            new PropertyMetadata(null));

    public NodeCanvasViewModel? ViewModel {
        get => (NodeCanvasViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(SearchPanel),
            new PropertyMetadata(null));

    public object? Content {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public override void OnApplyTemplate() {
        base.OnApplyTemplate();

        _searchBox  = GetTemplateChild("PART_SearchBox") as TextBox;
        _popup      = GetTemplateChild("PART_Popup") as Popup;
        _resultList = GetTemplateChild("PART_ResultList") as ListBox;

        if (_searchBox != null) {
            _searchBox.GotFocus       += OnSearchBoxGotFocus;
            _searchBox.LostFocus      += OnSearchBoxLostFocus;
            _searchBox.TextChanged    += OnSearchBoxTextChanged;
            _searchBox.PreviewKeyDown += OnSearchBoxPreviewKeyDown;
        }

        if (_resultList != null) {
            _resultList.MouseDoubleClick += OnResultListMouseDoubleClick;
        }
    }

    private void OnSearchBoxGotFocus(object sender, RoutedEventArgs e) {
        if (_searchBox is { Text: "검색..." }) {
            _searchBox.Text = string.Empty;
        }
    }

    private void OnSearchBoxLostFocus(object sender, RoutedEventArgs e) {
        if (_searchBox != null && string.IsNullOrWhiteSpace(_searchBox.Text)) {
            _searchBox.Text = "검색...";
        }
    }

    private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e) {
        if (_searchBox == null || _popup == null || _resultList == null || PluginService == null) return;

        var searchText = _searchBox.Text;
        if (string.IsNullOrWhiteSpace(searchText) || searchText == "검색...") {
            _popup.IsOpen = false;
            return;
        }

        // 메타데이터 기반 검색 결과 업데이트
        var results = PluginService.GetAllNodeMetadata()
                               .Where(m => m.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                                        || m.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                                        || m.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                               .OrderBy(m => m.Category)
                               .ThenBy(m => m.Name)
                               .ToList();

        _resultList.ItemsSource = results;
        _popup.IsOpen           = results.Any();
    }

    private void OnSearchBoxPreviewKeyDown(object sender, KeyEventArgs e) {
        if (_resultList == null) return;

        switch (e.Key) {
            case Key.Down:
                if (_resultList.SelectedIndex < _resultList.Items.Count - 1) {
                    _resultList.SelectedIndex++;
                }

                e.Handled = true;
                break;

            case Key.Up:
                if (_resultList.SelectedIndex > 0) {
                    _resultList.SelectedIndex--;
                }

                e.Handled = true;
                break;

            case Key.Enter:
                AddSelectedNode();
                e.Handled = true;
                break;

            case Key.Escape:
                if (_popup != null) {
                    _popup.IsOpen = false;
                }

                e.Handled = true;
                break;
        }
    }

    private void OnResultListMouseDoubleClick(object sender, MouseButtonEventArgs e) {
        AddSelectedNode();
    }

    private void AddSelectedNode() {
        if (_resultList == null || _popup == null || ViewModel == null || PluginService == null) return;

        var selectedMetadata = (NodeMetadata)_resultList.SelectedItem;
        var nodeType         = selectedMetadata.NodeType;
        ViewModel.AddNodeCommand.Execute(nodeType);
        _popup.IsOpen = false;
        if (_searchBox != null) {
            _searchBox.Text = "검색...";
        }
    }
}
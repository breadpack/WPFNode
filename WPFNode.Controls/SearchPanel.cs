using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;
using WPFNode.Core.Models;
using WPFNode.Core.Services;
using WPFNode.Core.ViewModels.Nodes;

namespace WPFNode.Controls;

[ContentProperty(nameof(Content))]
public class SearchPanel : Control
{
    private TextBox? _searchBox;
    private Popup? _popup;
    private ListBox? _resultList;

    static SearchPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchPanel),
            new FrameworkPropertyMetadata(typeof(SearchPanel)));
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(NodeCanvasViewModel),
            typeof(SearchPanel),
            new PropertyMetadata(null, OnViewModelChanged));

    public NodeCanvasViewModel? ViewModel
    {
        get => (NodeCanvasViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(SearchPanel),
            new PropertyMetadata(null));

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SearchPanel control)
        {
            control.DataContext = e.NewValue;
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _searchBox = GetTemplateChild("PART_SearchBox") as TextBox;
        _popup = GetTemplateChild("PART_Popup") as Popup;
        _resultList = GetTemplateChild("PART_ResultList") as ListBox;

        if (_searchBox != null)
        {
            _searchBox.GotFocus += OnSearchBoxGotFocus;
            _searchBox.LostFocus += OnSearchBoxLostFocus;
            _searchBox.TextChanged += OnSearchBoxTextChanged;
            _searchBox.PreviewKeyDown += OnSearchBoxPreviewKeyDown;
        }

        if (_resultList != null)
        {
            _resultList.SelectionChanged += OnResultListSelectionChanged;
            _resultList.MouseDoubleClick += OnResultListMouseDoubleClick;
        }
    }

    private void OnSearchBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (_searchBox != null && _searchBox.Text == "검색...")
        {
            _searchBox.Text = string.Empty;
        }
    }

    private void OnSearchBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (_searchBox != null && string.IsNullOrWhiteSpace(_searchBox.Text))
        {
            _searchBox.Text = "검색...";
        }
    }

    private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_searchBox == null || _popup == null || _resultList == null) return;

        var searchText = _searchBox.Text;
        if (string.IsNullOrWhiteSpace(searchText) || searchText == "검색...")
        {
            _popup.IsOpen = false;
            return;
        }

        // 검색 결과 업데이트
        var results = ViewModel?.SearchNodes(searchText);
        if (results != null)
        {
            _resultList.ItemsSource = results;
            _popup.IsOpen = true;
        }
    }

    private void OnSearchBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_resultList == null) return;

        switch (e.Key)
        {
            case Key.Down:
                if (_resultList.SelectedIndex < _resultList.Items.Count - 1)
                {
                    _resultList.SelectedIndex++;
                }
                e.Handled = true;
                break;

            case Key.Up:
                if (_resultList.SelectedIndex > 0)
                {
                    _resultList.SelectedIndex--;
                }
                e.Handled = true;
                break;

            case Key.Enter:
                AddSelectedNode();
                e.Handled = true;
                break;

            case Key.Escape:
                if (_popup != null)
                {
                    _popup.IsOpen = false;
                }
                e.Handled = true;
                break;
        }
    }

    private void OnResultListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 선택된 항목 미리보기 표시
    }

    private void OnResultListMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        AddSelectedNode();
    }

    private void AddSelectedNode()
    {
        if (_resultList == null || _popup == null || ViewModel == null) return;

        var selectedTemplate = _resultList.SelectedItem as NodeTemplate;
        if (selectedTemplate != null)
        {
            var node = selectedTemplate.CreateNode();
            ViewModel.AddNodeCommand.Execute(node);
            _popup.IsOpen = false;
        }
    }
} 

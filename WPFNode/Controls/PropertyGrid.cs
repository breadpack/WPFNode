using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Services;
using WPFNode.ViewModels;
using WPFNode.ViewModels.Nodes;
using NodeCanvasViewModel = WPFNode.ViewModels.Nodes.NodeCanvasViewModel;
using System.Collections.Specialized;
using System.Reflection;

namespace WPFNode.Controls;

public class PropertyGrid : Control, INotifyPropertyChanged, IDisposable
{
    private string _searchText = string.Empty;
    private NodeViewModel? _selectedNode;
    private ListCollectionView? _filteredProperties;
    private NodeCanvasViewModel? _canvasViewModel;
    private List<NodePropertyViewModel> _currentPropertyViewModels = new List<NodePropertyViewModel>();
    private bool _isDisposed;

    public event PropertyChangedEventHandler? PropertyChanged;

    static PropertyGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGrid),
            new FrameworkPropertyMetadata(typeof(PropertyGrid)));
    }

    public PropertyGrid()
    {
        UpdateProperties();
    }

    ~PropertyGrid()
    {
        Dispose(false);
    }

    public static readonly DependencyProperty CanvasViewModelProperty =
        DependencyProperty.Register(
            nameof(CanvasViewModel),
            typeof(NodeCanvasViewModel),
            typeof(PropertyGrid),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.None,
                OnCanvasViewModelChanged));

    public NodeCanvasViewModel? CanvasViewModel
    {
        get => (NodeCanvasViewModel?)GetValue(CanvasViewModelProperty);
        set => SetValue(CanvasViewModelProperty, value);
    }

    private static void OnCanvasViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyGrid propertyGrid)
        {
            // 이전 CanvasViewModel의 SelectedItems 컬렉션 변경 이벤트 구독 해제
            if (e.OldValue is NodeCanvasViewModel oldViewModel)
            {
                ((INotifyCollectionChanged)oldViewModel.SelectedItems).CollectionChanged -= 
                    propertyGrid.OnSelectedItemsCollectionChanged;
            }

            // 이전 속성 뷰모델 정리
            foreach (var viewModel in propertyGrid._currentPropertyViewModels)
            {
                viewModel.Cleanup();
            }
            propertyGrid._currentPropertyViewModels.Clear();

            // 이전 선택 노드의 이벤트 구독 해제
            if (propertyGrid._selectedNode != null)
            {
                propertyGrid._selectedNode.PropertyChanged -= propertyGrid.OnSelectedNodePropertyChanged;
                propertyGrid._selectedNode = null;
            }

            propertyGrid._canvasViewModel = e.NewValue as NodeCanvasViewModel;
            
            // 새 CanvasViewModel의 SelectedItems 컬렉션 변경 이벤트 구독
            if (propertyGrid._canvasViewModel != null)
            {
                ((INotifyCollectionChanged)propertyGrid._canvasViewModel.SelectedItems).CollectionChanged += 
                    propertyGrid.OnSelectedItemsCollectionChanged;
            }
            
            propertyGrid.UpdateSelectedNode();
        }
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateSelectedNode();
        UpdateProperties();
    }

    private void UpdateSelectedNode()
    {
        // SelectedItems에서 첫 번째 NodeViewModel을 찾아 SelectedNode로 설정
        if (_canvasViewModel != null)
        {
            var selectedNode = _canvasViewModel.SelectedItems
                .OfType<NodeViewModel>()
                .FirstOrDefault();
            
            if (_selectedNode != selectedNode)
            {
                // 이전 노드의 이벤트 구독 해제
                if (_selectedNode != null)
                {
                    _selectedNode.PropertyChanged -= OnSelectedNodePropertyChanged;
                    
                    // 이전 속성 뷰모델 정리
                    foreach (var viewModel in _currentPropertyViewModels)
                    {
                        viewModel.Cleanup();
                    }
                    _currentPropertyViewModels.Clear();
                }
                
                _selectedNode = selectedNode;
                
                if (_selectedNode != null)
                {
                    _selectedNode.PropertyChanged += OnSelectedNodePropertyChanged;
                }
                
                OnPropertyChanged(nameof(SelectedNode));
                
                // 노드가 변경되었으므로 속성 목록 갱신
                UpdateProperties();
            }
        }
    }

    public NodeViewModel? SelectedNode => _selectedNode;

    private void OnSelectedNodePropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(NodeViewModel.Properties)) {
            // 속성 목록이 변경되었으므로 이전 속성 뷰모델 정리 후 업데이트
            foreach (var viewModel in _currentPropertyViewModels)
            {
                viewModel.Cleanup();
            }
            _currentPropertyViewModels.Clear();
            
            UpdateProperties();
        }        
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                _filteredProperties?.Refresh();
            }
        }
    }

    public ICollectionView? FilteredProperties => _filteredProperties;

    private void UpdateProperties()
    {
        // 이전 NodePropertyViewModel 인스턴스의 이벤트 구독 해제
        foreach (var viewModel in _currentPropertyViewModels)
        {
            viewModel.Cleanup();
        }
        
        // 새로운 NodePropertyViewModel 인스턴스 생성
        var properties = _selectedNode?.Model?.Properties
            .Select(p => new NodePropertyViewModel(p))
            .ToList() ?? new List<NodePropertyViewModel>();
        
        // 현재 인스턴스 목록 업데이트
        _currentPropertyViewModels = properties;

        _filteredProperties = new ListCollectionView(properties)
        {
            Filter = OnFilterProperties
        };

        OnPropertyChanged(nameof(FilteredProperties));
    }

    private bool OnFilterProperties(object item)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        if (item is NodePropertyViewModel propertyItem)
        {
            return propertyItem.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == DataContextProperty)
        {
            UpdatePropertyControls();
        }
    }

    private void UpdatePropertyControls()
    {
        // DataContext가 INodeProperty인 경우, 이 속성에 대한 컨트롤을 생성하고 
        // 현재 속성 목록에 추가하거나 기존 필터링된 속성 목록을 업데이트
        if (DataContext is INodeProperty property)
        {
            // 1. 새로운 NodePropertyViewModel 생성
            var propertyViewModel = new NodePropertyViewModel(property);
            
            // 2. 기존 _currentPropertyViewModels 목록 정리
            foreach (var vm in _currentPropertyViewModels)
            {
                vm.Cleanup();
            }
            _currentPropertyViewModels.Clear();
            
            // 3. 새 목록에 추가
            _currentPropertyViewModels.Add(propertyViewModel);
            
            // 4. FilteredProperties 업데이트
            _filteredProperties = new ListCollectionView(_currentPropertyViewModels)
            {
                Filter = OnFilterProperties
            };
            
            // 5. 변경 알림
            OnPropertyChanged(nameof(FilteredProperties));
        }
        else
        {
            // DataContext가 INodeProperty가 아닌 경우, 빈 목록으로 설정
            if (_currentPropertyViewModels.Count > 0)
            {
                foreach (var vm in _currentPropertyViewModels)
                {
                    vm.Cleanup();
                }
                _currentPropertyViewModels.Clear();
                
                _filteredProperties = new ListCollectionView(_currentPropertyViewModels)
                {
                    Filter = OnFilterProperties
                };
                
                OnPropertyChanged(nameof(FilteredProperties));
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            // 관리되는 리소스 해제
            if (_selectedNode != null)
            {
                _selectedNode.PropertyChanged -= OnSelectedNodePropertyChanged;
                _selectedNode = null;
            }

            if (_canvasViewModel != null)
            {
                ((INotifyCollectionChanged)_canvasViewModel.SelectedItems).CollectionChanged -= 
                    OnSelectedItemsCollectionChanged;
                _canvasViewModel = null;
            }

            // 속성 뷰모델 정리
            foreach (var viewModel in _currentPropertyViewModels)
            {
                viewModel.Cleanup();
            }
            _currentPropertyViewModels.Clear();
        }

        _isDisposed = true;
    }
}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.ViewModels.Base;

namespace WPFNode.ViewModels.Nodes;

public class NodeGroupViewModel : ViewModelBase, ISelectable, IDisposable
{
    private readonly NodeGroup _model;
    private readonly NodeCanvasViewModel _canvas;
    private string _name;
    private double _x;
    private double _y;
    private double _width;
    private double _height;
    private bool _isCollapsed;
    private Color _color;

    public NodeGroupViewModel(NodeGroup model, NodeCanvasViewModel canvas)
    {
        _model = model;
        _canvas = canvas;
        _name = model.Name;
        
        // 그룹 멤버를 기반으로 초기 크기와 위치 계산
        UpdateBoundingBox();

        // Model 속성 변경 감지
        _model.PropertyChanged += OnModelOnPropertyChanged;
    }

    public void Dispose() {
        _model.PropertyChanged -= OnModelOnPropertyChanged;
    }

    private void OnModelOnPropertyChanged(object? s, PropertyChangedEventArgs e) {
        switch (e.PropertyName) {
            case nameof(NodeGroup.Name):
                Name = _model.Name;
                break;
            case nameof(NodeGroup.X):
                X = _model.X;
                break;
            case nameof(NodeGroup.Y):
                Y = _model.Y;
                break;
            case nameof(NodeGroup.Width):
                Width = _model.Width;
                break;
            case nameof(NodeGroup.Height):
                Height = _model.Height;
                break;
            case nameof(NodeGroup.IsCollapsed):
                IsCollapsed = _model.IsCollapsed;
                break;
            case nameof(NodeGroup.Alpha) or nameof(NodeGroup.Red) or nameof(NodeGroup.Green) or nameof(NodeGroup.Blue):
                Color = Color.FromArgb(_model.Alpha, _model.Red, _model.Green, _model.Blue);
                break;
            case nameof(NodeGroup.Nodes):
                OnModelNodesChanged(s, EventArgs.Empty);
                break;
        }
    }

    public NodeGroup Model => _model;

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                _model.Name = value;
            }
        }
    }

    public bool IsSelected
    {
        get => _canvas.IsItemSelected(this);
    }

    /// <summary>
    /// 그룹을 선택합니다.
    /// </summary>
    /// <param name="clearOthers">다른 항목의 선택을 해제할지 여부</param>
    public void Select(bool clearOthers = true)
    {
        _canvas.SelectItem(this, clearOthers);
        OnPropertyChanged(nameof(IsSelected));
    }

    /// <summary>
    /// 그룹의 선택을 해제합니다.
    /// </summary>
    public void Deselect()
    {
        _canvas.DeselectItem(this);
        OnPropertyChanged(nameof(IsSelected));
    }

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            if (SetProperty(ref _isCollapsed, value))
            {
                _model.IsCollapsed = value;
            }
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (SetProperty(ref _color, value))
            {
                _model.Alpha = value.A;
                _model.Red = value.R;
                _model.Green = value.G;
                _model.Blue = value.B;
            }
        }
    }

    public ObservableCollection<NodeViewModel> Nodes
    {
        get
        {
            var result = new ObservableCollection<NodeViewModel>();
            foreach (var node in _model.Nodes)
            {
                var nodeVM = _canvas.Nodes.FirstOrDefault(vm => vm.Model == node);
                if (nodeVM != null)
                {
                    result.Add(nodeVM);
                }
            }
            return result;
        }
    }
    
    // ISelectable 인터페이스 구현
    public Guid Id => _model.Id;
    public string SelectionType => "Group";

    private void OnModelNodesChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Nodes));
        UpdateBoundingBox();
    }

    private void UpdateBoundingBox()
    {
        if (_model.Nodes.Count == 0)
        {
            X = 0;
            Y = 0;
            Width = 100;
            Height = 100;
            return;
        }

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var node in _model.Nodes)
        {
            minX = Math.Min(minX, node.X);
            minY = Math.Min(minY, node.Y);
            maxX = Math.Max(maxX, node.X + 200); // 노드 크기를 고려한 임의의 값
            maxY = Math.Max(maxY, node.Y + 100); // 노드 크기를 고려한 임의의 값
        }

        // 여백 추가
        const int padding = 10;
        X = minX - padding;
        Y = minY - padding;
        Width = maxX - minX + (padding * 2);
        Height = maxY - minY + (padding * 2);
    }
} 
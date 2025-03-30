using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using WPFNode.Constants;

namespace WPFNode.Models;

public class NodeGroup : INotifyPropertyChanged
{
    private string _name;
    private double _x;
    private double _y;
    private double _width;
    private double _height;
    private bool _isCollapsed;
    private byte _Alpha = 255;
    private byte _Red = 173;
    private byte _Green = 216;
    private byte _Blue = 230;
    private readonly List<NodeBase> _nodes = new();

    [JsonConstructor]
    public NodeGroup(Guid id, string name)
    {
        Id      = id;
        _name   = name;
        _width  = 200;
        _height = 150;
        _nodes  = new();
    }

    public Guid Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public double X
    {
        get => _x;
        set
        {
            if (Math.Abs(_x - value) > double.Epsilon)
            {
                _x = value;
                OnPropertyChanged();
            }
        }
    }

    public double Y
    {
        get => _y;
        set
        {
            if (Math.Abs(_y - value) > double.Epsilon)
            {
                _y = value;
                OnPropertyChanged();
            }
        }
    }

    public double Width
    {
        get => _width;
        set
        {
            if (Math.Abs(_width - value) > double.Epsilon)
            {
                _width = value;
                OnPropertyChanged();
            }
        }
    }

    public double Height
    {
        get => _height;
        set
        {
            if (Math.Abs(_height - value) > double.Epsilon)
            {
                _height = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            if (_isCollapsed != value)
            {
                _isCollapsed = value;
                foreach (var node in Nodes)
                {
                    node.IsVisible = !value;
                }
                UpdateBounds();
                OnPropertyChanged();
            }
        }
    }
    
    public byte Alpha
    {
        get => _Alpha;
        set
        {
            if (_Alpha != value)
            {
                _Alpha = value;
                OnPropertyChanged();
            }
        }
    }
    
    public byte Red
    {
        get => _Red;
        set
        {
            if (_Red != value)
            {
                _Red = value;
                OnPropertyChanged();
            }
        }
    }
    
    public byte Green
    {
        get => _Green;
        set
        {
            if (_Green != value)
            {
                _Green = value;
                OnPropertyChanged();
            }
        }
    }
    
    public byte Blue
    {
        get => _Blue;
        set
        {
            if (_Blue != value)
            {
                _Blue = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isAutoSize = true;
    public bool IsAutoSize
    {
        get => _isAutoSize;
        set
        {
            if (_isAutoSize != value)
            {
                _isAutoSize = value;
                if (value)
                {
                    UpdateBounds();
                }
                OnPropertyChanged();
            }
        }
    }

    public IReadOnlyList<NodeBase> Nodes => _nodes;

    private void UpdateBounds()
    {
        if (!Nodes.Any() || IsCollapsed)
        {
            Width = 150;  // 접혀있을 때의 기본 크기
            Height = 30;
            return;
        }

        if (!IsAutoSize) return;  // 수동 크기 조절 모드에서는 자동 크기 조정 안 함

        var minX = Nodes.Min(n => n.X);
        var minY = Nodes.Min(n => n.Y);
        var maxX = Nodes.Max(n => n.X);
        var maxY = Nodes.Max(n => n.Y);

        X = minX - 10;  // 여백 추가
        Y = minY - 10;
        Width = maxX - minX + 20;
        Height = maxY - minY + 20;
    }

    public void ResizeAndMoveNodes(double newX, double newY, double newWidth, double newHeight)
    {
        if (IsCollapsed || !Nodes.Any()) return;

        var oldX = X;
        var oldY = Y;
        var oldWidth = Width;
        var oldHeight = Height;

        // 노드들의 상대적 위치를 유지하면서 크기 조절
        foreach (var node in Nodes)
        {
            var relativeX = (node.X - oldX) / oldWidth;
            var relativeY = (node.Y - oldY) / oldHeight;

            node.X = newX + (relativeX * newWidth);
            node.Y = newY + (relativeY * newHeight);
        }

        // 그룹 크기 업데이트
        X = newX;
        Y = newY;
        Width = newWidth;
        Height = newHeight;

        IsAutoSize = false;  // 수동 크기 조절 모드로 전환
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Add(NodeBase node) {
        if (node == null) throw new ArgumentNullException(nameof(node));
        _nodes.Add(node);
        UpdateBounds();
        OnPropertyChanged(nameof(Nodes));
    }
} 

using System.Windows.Media;
using WPFNode.Core.Models;
using WPFNode.Core.ViewModels.Base;

namespace WPFNode.Core.ViewModels.Nodes;

public class NodeGroupViewModel : ViewModelBase
{
    private readonly NodeGroup _group;
    private string _name;
    private double _x;
    private double _y;
    private double _width;
    private double _height;
    private bool _isCollapsed;
    private Color _color;

    public NodeGroupViewModel(NodeGroup group, NodeCanvasViewModel nodeCanvasViewModel)
    {
        _group = group;
        _name = group.Name;
        _x = group.X;
        _y = group.Y;
        _width = group.Width;
        _height = group.Height;
        _isCollapsed = group.IsCollapsed;
        _color = group.Color;

        // Model 속성 변경 감지
        _group.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(NodeGroup.Name):
                    Name = _group.Name;
                    break;
                case nameof(NodeGroup.X):
                    X = _group.X;
                    break;
                case nameof(NodeGroup.Y):
                    Y = _group.Y;
                    break;
                case nameof(NodeGroup.Width):
                    Width = _group.Width;
                    break;
                case nameof(NodeGroup.Height):
                    Height = _group.Height;
                    break;
                case nameof(NodeGroup.IsCollapsed):
                    IsCollapsed = _group.IsCollapsed;
                    break;
                case nameof(NodeGroup.Color):
                    Color = _group.Color;
                    break;
            }
        };
    }

    public Guid Id => _group.Id;

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                _group.Name = value;
            }
        }
    }

    public double X
    {
        get => _x;
        set
        {
            if (SetProperty(ref _x, value))
            {
                _group.X = value;
            }
        }
    }

    public double Y
    {
        get => _y;
        set
        {
            if (SetProperty(ref _y, value))
            {
                _group.Y = value;
            }
        }
    }

    public double Width
    {
        get => _width;
        set
        {
            if (SetProperty(ref _width, value))
            {
                _group.Width = value;
            }
        }
    }

    public double Height
    {
        get => _height;
        set
        {
            if (SetProperty(ref _height, value))
            {
                _group.Height = value;
            }
        }
    }

    public bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            if (SetProperty(ref _isCollapsed, value))
            {
                _group.IsCollapsed = value;
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
                _group.Color = value;
            }
        }
    }

    public NodeGroup Model => _group;
} 
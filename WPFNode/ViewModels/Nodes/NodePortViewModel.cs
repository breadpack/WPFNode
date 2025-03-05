using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.ViewModels.Base;

namespace WPFNode.ViewModels.Nodes;

public class NodePortViewModel : ViewModelBase, IEquatable<NodePortViewModel>
{
    private readonly IPort _port;
    private bool _isSelected;
    private readonly NodeCanvasViewModel _canvas;

    public NodePortViewModel(IPort port, NodeCanvasViewModel canvas)
    {
        _port = port;
        _canvas = canvas;

        // Model 속성 변경 감지
        _port.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(IPort.Connections):
                    OnPropertyChanged(nameof(Connections));
                    OnPropertyChanged(nameof(IsConnected));
                    break;
                case nameof(IPort.IsVisible):
                    OnPropertyChanged(nameof(IsVisible));
                    break;
            }
        };
    }

    public PortId Id => _port.Id;
    
    public string Name
    {
        get => _port.Name;
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public bool IsInput => _port.IsInput;
    public Type DataType => _port.DataType;
    public bool IsConnected => _port.IsConnected;
    public IReadOnlyList<ConnectionViewModel> Connections => 
        _port.Connections
            .Select(c => _canvas.Connections.FirstOrDefault(vm => vm.Model == c))
            .Where(vm => vm != null)
            .Cast<ConnectionViewModel>()
            .ToList();

    public bool IsVisible => _port.IsVisible;

    public bool CanConnectTo(NodePortViewModel other)
    {
        // 입력 포트에서 출력 포트로의 연결
        if (IsInput && !other.IsInput)
        {
            return _port is IInputPort inputPort && other._port is IOutputPort outputPort && 
                   outputPort.CanConnectTo(inputPort);
        }

        // 출력 포트에서 입력 포트로의 연결
        if (!IsInput && other.IsInput)
        {
            return other._port is IInputPort inputPort && _port is IOutputPort outputPort && 
                   outputPort.CanConnectTo(inputPort);
        }

        return false;
    }

    public IPort Model => _port;

    public NodeViewModel? Parent { get; internal set; }

    public override bool Equals(object? obj)
    {
        if (obj is NodePortViewModel other)
            return Equals(other);
        return false;
    }

    public bool Equals(NodePortViewModel? other)
    {
        if (other is null) return false;
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(NodePortViewModel left, NodePortViewModel? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(NodePortViewModel? left, NodePortViewModel? right)
    {
        return !(left == right);
    }
} 
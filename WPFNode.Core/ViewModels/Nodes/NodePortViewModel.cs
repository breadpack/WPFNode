using WPFNode.Core.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;
using WPFNode.Plugin.SDK;
using WPFNode.Abstractions;

namespace WPFNode.Core.ViewModels.Nodes;

public class NodePortViewModel : ViewModelBase, IEquatable<NodePortViewModel>
{
    private readonly IPort _port;
    private string _name;
    private object? _value;
    private bool _isSelected;
    private readonly ObservableCollection<ConnectionViewModel> _connections;
    private readonly NodeCanvasViewModel _canvas;

    public NodePortViewModel(IPort port, NodeCanvasViewModel canvas)
    {
        _port = port;
        _canvas = canvas;
        _name = port.Name;
        _value = port.Value;
        _connections = new ObservableCollection<ConnectionViewModel>();

        // 초기 연결 상태 설정
        UpdateConnections();

        // Model 속성 변경 감지
        _port.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(IPort.Name):
                    Name = _port.Name;
                    break;
                case nameof(IPort.Value):
                    Value = _port.Value;
                    break;
                case nameof(IPort.Connections):
                    UpdateConnections();
                    break;
            }
        };
    }

    private void UpdateConnections()
    {
        _connections.Clear();
        foreach (var connection in _port.Connections)
        {
            var vm = _canvas.Connections.FirstOrDefault(c => c.Model == connection);
            if (vm != null)
            {
                _connections.Add(vm);
            }
        }
    }

    public Guid Id => _port.Id;
    
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                _port.Name = value;
            }
        }
    }

    public object? Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                _port.Value = value;
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public bool IsInput => _port.IsInput;
    public Type DataType => _port.DataType;
    public bool IsConnected => _port.IsConnected;
    public IReadOnlyList<ConnectionViewModel> Connections => _connections;

    public bool CanConnectTo(NodePortViewModel other)
    {
        // 입력-출력 포트 방향이 맞는지 확인
        if (IsInput == other.IsInput) return false;

        // 같은 노드의 포트인지 확인
        var parent = Parent;
        var otherParent = other.Parent;
        if (parent != null && parent == otherParent) return false;

        // 데이터 타입 호환성 확인
        if (IsInput)
        {
            return DataType.IsAssignableFrom(other.DataType);
        }
        return other.DataType.IsAssignableFrom(DataType);
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
using System.ComponentModel;
using System.Windows;
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
                case nameof(IPort.DataType):
                    OnPropertyChanged(nameof(DataType));
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
    
    public bool IsInput     => _port.IsInput;
    public Type DataType    => _port.DataType;
    public bool IsConnected => _port.IsConnected;
    public IReadOnlyList<ConnectionViewModel> Connections => 
        _port.Connections
            .Select(c => _canvas.Connections.FirstOrDefault(vm => vm.Model == c))
            .Where(vm => vm != null)
            .Cast<ConnectionViewModel>()
            .ToList();

    public bool IsVisible => _port.IsVisible;

    /// <summary>
    /// 포트가 Flow 포트인지 여부를 가져옵니다.
    /// </summary>
    public bool IsFlow => _port is IFlowOutPort or IFlowInPort;

    public bool CanConnectTo(NodePortViewModel other)
    {
        // 같은 노드의 포트끼리는 연결할 수 없음
        if (Parent == other.Parent)
            return false;

        // 입력/출력 방향이 같으면 연결할 수 없음
        if (IsInput == other.IsInput)
            return false;
            
        // Flow 포트와 데이터 포트는 서로 연결할 수 없음
        if (IsFlow != other.IsFlow)
            return false;
            
        // 데이터 포트인 경우 타입 호환성 검사
        if (!IsFlow)
        {
            if(Model is not IOutputPort outputPort || other.Model is not IInputPort inputPort)
                return false;
            
            // 둘 다 데이터 포트일 때만 타입 검사
            if (!outputPort.CanConnectTo(inputPort))
            {
                return false;
            }
        }

        // 모든 검사를 통과하면 연결 가능
        return true;
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
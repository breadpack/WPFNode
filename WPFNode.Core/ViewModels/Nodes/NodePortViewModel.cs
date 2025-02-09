using WPFNode.Core.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;
using WPFNode.Plugin.SDK;
using WPFNode.Abstractions;

namespace WPFNode.Core.ViewModels.Nodes;

public class NodePortViewModel : ViewModelBase
{
    private readonly IPort _port;
    private string _name;
    private object? _value;
    private bool _isSelected;
    private readonly ObservableCollection<ConnectionViewModel> _connections;

    public NodePortViewModel(IPort port)
    {
        _port = port;
        _name = port.Name;
        _value = port.Value;
        _connections = new ObservableCollection<ConnectionViewModel>();

        // 초기 연결 상태 설정
        foreach (var connection in port.Connections)
        {
            _connections.Add(new ConnectionViewModel(connection));
        }

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
        var currentConnections = _connections.Select(c => c.Model).ToList();
        var modelConnections = _port.Connections.ToList();

        // 제거된 연결 처리
        foreach (var connection in currentConnections.Where(c => !modelConnections.Contains(c)))
        {
            var vm = _connections.FirstOrDefault(c => c.Model == connection);
            if (vm != null)
                _connections.Remove(vm);
        }

        // 새로운 연결 처리
        foreach (var connection in modelConnections.Where(c => !currentConnections.Contains(c)))
        {
            _connections.Add(new ConnectionViewModel(connection));
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
        if (IsInput == other.IsInput) return false;
        if (IsInput)
        {
            return DataType.IsAssignableFrom(other.DataType);
        }
        return other.DataType.IsAssignableFrom(DataType);
    }

    public IPort Model => _port;
} 
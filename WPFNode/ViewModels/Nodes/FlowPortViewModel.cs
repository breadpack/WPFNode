using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WPFNode.Interfaces;
using WPFNode.Interfaces.Flow;
using WPFNode.Models;
using WPFNode.Models.Flow;

namespace WPFNode.ViewModels.Nodes;

/// <summary>
/// 흐름 포트를 위한 ViewModel
/// </summary>
public class FlowPortViewModel : INotifyPropertyChanged
{
    private readonly IFlowPort _port;
    private bool _isConnecting;
    private bool _isHighlighted;
    private ObservableCollection<FlowConnectionViewModel> _connections = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="port">포트 모델</param>
    public FlowPortViewModel(IFlowPort port)
    {
        _port = port ?? throw new ArgumentNullException(nameof(port));
        
        if (port is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += OnPortPropertyChanged;
        }
        
        // 기존 연결 초기화
        foreach (var connection in _port.Connections)
        {
            var viewModel = new FlowConnectionViewModel(connection);
            Connections.Add(viewModel);
        }
    }

    /// <summary>
    /// 포트 모델
    /// </summary>
    public IFlowPort Port => _port;
    
    /// <summary>
    /// 포트 이름
    /// </summary>
    public string Name => (_port as FlowPort)?.Name ?? string.Empty;
    
    /// <summary>
    /// 포트 소유 노드
    /// </summary>
    public INode? Node => _port.Node;
    
    /// <summary>
    /// 포트가 입력 포트인지 여부
    /// </summary>
    public bool IsInput => _port is IFlowInPort;
    
    /// <summary>
    /// 포트가 출력 포트인지 여부
    /// </summary>
    public bool IsOutput => _port is IFlowOutPort;
    
    /// <summary>
    /// 포트가 연결 중인지 여부
    /// </summary>
    public bool IsConnecting
    {
        get => _isConnecting;
        set => SetField(ref _isConnecting, value);
    }
    
    /// <summary>
    /// 포트가 강조 표시되는지 여부
    /// </summary>
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetField(ref _isHighlighted, value);
    }

    /// <summary>
    /// 포트 위치
    /// </summary>
    public Point Position { get; set; }

    /// <summary>
    /// 연결 목록
    /// </summary>
    public ObservableCollection<FlowConnectionViewModel> Connections => _connections;
    
    /// <summary>
    /// 연결 여부
    /// </summary>
    public bool IsConnected => _port.Connections.Count > 0;

    private void OnPortPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // FlowPort has no IsConnected property, instead refresh when Connections changes
        if (e.PropertyName == nameof(IFlowPort.Connections))
        {
            OnPropertyChanged(nameof(IsConnected));
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

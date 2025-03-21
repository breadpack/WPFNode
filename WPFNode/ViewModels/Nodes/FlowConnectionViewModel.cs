using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WPFNode.Interfaces.Flow;
using WPFNode.Models.Flow;

namespace WPFNode.ViewModels.Nodes;

/// <summary>
/// 흐름 연결에 대한 ViewModel
/// </summary>
public class FlowConnectionViewModel : INotifyPropertyChanged
{
    private readonly IFlowConnection _connection;
    private bool _isSelected;
    private bool _isHighlighted;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="connection">연결 모델</param>
    public FlowConnectionViewModel(IFlowConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    /// 연결 모델
    /// </summary>
    public IFlowConnection Connection => _connection;

    /// <summary>
    /// 소스 포트
    /// </summary>
    public IFlowOutPort Source => _connection.Source;

    /// <summary>
    /// 타겟 포트
    /// </summary>
    public IFlowInPort Target => _connection.Target;

    /// <summary>
    /// 연결이 선택되었는지 여부
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <summary>
    /// 연결이 강조 표시되는지 여부
    /// </summary>
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetField(ref _isHighlighted, value);
    }

    /// <summary>
    /// 소스 위치
    /// </summary>
    public Point SourcePosition { get; set; }

    /// <summary>
    /// 타겟 위치
    /// </summary>
    public Point TargetPosition { get; set; }

    /// <summary>
    /// 연결 ID
    /// </summary>
    public Guid Id => _connection.Guid;

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

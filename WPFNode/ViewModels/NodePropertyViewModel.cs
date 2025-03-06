using System.ComponentModel;
using System.Windows;
using WPFNode.Interfaces;
using WPFNode.Services;

namespace WPFNode.ViewModels;

public class NodePropertyViewModel : INotifyPropertyChanged
{
    private readonly INodeProperty _property;
    private FrameworkElement? _control;
    private PropertyChangedEventHandler? _propertyChangedHandler;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NodePropertyViewModel(INodeProperty property)
    {
        _property = property;
        _control = NodeServices.PropertyControlProviderRegistry.CreateControl(property);

        if (_property is INotifyPropertyChanged notifyPropertyChanged)
        {
            _propertyChangedHandler = (s, e) =>
            {
                if (e.PropertyName == nameof(Value))
                {
                    OnPropertyChanged(nameof(Value));
                }
                else if (e.PropertyName == nameof(CanConnectToPort))
                {
                    OnPropertyChanged(nameof(CanConnectToPort));
                }
                else if (e.PropertyName == nameof(IInputPort.IsVisible) && _property is IInputPort)
                {
                    OnPropertyChanged(nameof(IsVisible));
                }
            };
            
            notifyPropertyChanged.PropertyChanged += _propertyChangedHandler;
        }
    }

    public string                  DisplayName => _property.DisplayName;
    public string?                 Format      => _property.Format;
    
    public bool CanConnectToPort
    {
        get => _property.CanConnectToPort;
        set
        {
            if (_property.CanConnectToPort != value && _property is IInputPort inputPort)
            {
                // 연결 해제
                if (!value && inputPort.IsConnected)
                {
                    inputPort.Disconnect();
                }
                
                // 포트 연결 가능 여부 설정
                _property.CanConnectToPort = value;
                OnPropertyChanged(nameof(CanConnectToPort));
            }
        }
    }
    
    public bool IsConnected => _property is IInputPort { IsConnected: true };

    public bool IsVisible
    {
        get => _property is not IInputPort inputPort || inputPort.IsVisible;
        set
        {
            if (_property is IInputPort inputPort)
            {
                inputPort.IsVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public object? Value
    {
        get => _property.Value;
        set
        {
            if (!Equals(_property.Value, value))
            {
                _property.Value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public FrameworkElement? Control => _control;

    /// <summary>
    /// 이벤트 구독을 해제하고 리소스를 정리합니다.
    /// </summary>
    public void Cleanup()
    {
        if (_property is INotifyPropertyChanged notifyPropertyChanged && _propertyChangedHandler != null)
        {
            notifyPropertyChanged.PropertyChanged -= _propertyChangedHandler;
            _propertyChangedHandler = null;
        }
        
        // 컨트롤 참조 해제
        _control = null;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
using System.ComponentModel;
using System.Windows;
using WPFNode.Abstractions;
using WPFNode.Abstractions.Constants;

namespace WPFNode.Controls;

public class NodePropertyViewModel : INotifyPropertyChanged
{
    private readonly INodeProperty _property;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NodePropertyViewModel(INodeProperty property)
    {
        _property = property;

        if (_property is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += (s, e) =>
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
        }
    }

    public string                  DisplayName => _property.DisplayName;
    public NodePropertyControlType ControlType => _property.ControlType;
    public string?                 Format      => _property.Format;
    
    public bool CanConnectToPort
    {
        get => _property.CanConnectToPort;
        set
        {
            if (_property.CanConnectToPort != value)
            {
                // 연결 해제
                if (!value && _property.IsConnectedToPort)
                {
                    _property.DisconnectFromPort();
                }
                
                // 포트 연결 가능 여부 설정
                _property.CanConnectToPort = value;
                OnPropertyChanged(nameof(CanConnectToPort));
            }
        }
    }
    
    public bool IsConnected => _property.IsConnectedToPort;

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

    public DataTemplate? ControlTemplate
    {
        get
        {
            var key = ControlType switch
            {
                NodePropertyControlType.TextBox       => "TextBoxTemplate",
                NodePropertyControlType.NumberBox     => "NumberBoxTemplate",
                NodePropertyControlType.CheckBox      => "CheckBoxTemplate",
                NodePropertyControlType.ColorPicker   => "ColorPickerTemplate",
                NodePropertyControlType.ComboBox      => "ComboBoxTemplate",
                NodePropertyControlType.MultilineText => "MultilineTextTemplate",
                _                                     => "TextBoxTemplate"
            };

            return Application.Current.FindResource(key) as DataTemplate;
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
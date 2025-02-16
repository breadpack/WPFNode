using System;
using System.ComponentModel;
using WPFNode.Abstractions;
using WPFNode.Abstractions.Constants;

namespace WPFNode.Core.Models.Properties;

public class NodeProperty<T> : INodeProperty, INotifyPropertyChanged
{
    private readonly Func<T> _getValue;
    private readonly Action<T> _setValue;
    private T _value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NodeProperty(
        string displayName,
        NodePropertyControlType controlType,
        Func<T> getValue,
        Action<T> setValue,
        string? format = null,
        bool canConnectToPort = false)
    {
        DisplayName = displayName;
        ControlType = controlType;
        Format = format;
        CanConnectToPort = canConnectToPort;
        _getValue = getValue;
        _setValue = setValue;
        _value = getValue();
    }

    public string DisplayName { get; }
    public NodePropertyControlType ControlType { get; }
    public string? Format { get; }
    public bool CanConnectToPort { get; }

    public T Value
    {
        get => _value;
        set
        {
            if (!Equals(_value, value))
            {
                _value = value;
                _setValue(value);
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public object? GetValue() => Value;

    public void SetValue(object? value)
    {
        if (value is T typedValue)
        {
            Value = typedValue;
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 
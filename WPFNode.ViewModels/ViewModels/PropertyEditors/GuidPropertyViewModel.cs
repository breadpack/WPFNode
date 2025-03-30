using System;
using System.ComponentModel;
using System.Windows.Input;
using WPFNode.Commands;
using WPFNode.Interfaces;

namespace WPFNode.ViewModels.PropertyEditors;

public class GuidPropertyViewModel : INotifyPropertyChanged
{
    private readonly INodeProperty _property;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public GuidPropertyViewModel(INodeProperty property)
    {
        _property = property;
        GenerateGuidCommand = new SimpleCommand(GenerateNewGuid);
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

    public bool IsConnected => _property is IInputPort inputPort && inputPort.IsConnected;

    private void GenerateNewGuid()
    {
        Value = Guid.NewGuid();
    }

    public System.Windows.Input.ICommand GenerateGuidCommand { get; }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 
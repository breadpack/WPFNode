using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using WPFNode.Commands;
using WPFNode.Interfaces;

namespace WPFNode.ViewModels.PropertyEditors;

public class EnumFlagsPropertyViewModel : INotifyPropertyChanged
{
    private readonly INodeProperty _property;
    private readonly List<EnumValueViewModel> _enumValues = new();
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public EnumFlagsPropertyViewModel(INodeProperty property)
    {
        _property = property;
        
        if (property.PropertyType.IsEnum)
        {
            InitializeEnumValues();
        }
    }

    public ReadOnlyCollection<EnumValueViewModel> EnumValues => _enumValues.AsReadOnly();

    private void InitializeEnumValues()
    {
        _enumValues.Clear();
        
        var enumType = _property.PropertyType;
        var enumValues = Enum.GetValues(enumType);
        
        foreach (var value in enumValues)
        {
            if (Convert.ToInt64(value) != 0) // 0 값은 일반적으로 'None'이므로 제외
            {
                var displayName = Enum.GetName(enumType, value) ?? value.ToString();
                var isSelected = IsValueSelected(value);
                
                var enumValueViewModel = new EnumValueViewModel(displayName, value, isSelected, this);
                _enumValues.Add(enumValueViewModel);
            }
        }
    }

    private bool IsValueSelected(object enumValue)
    {
        if (_property.Value == null)
            return false;
        
        var currentValue = Convert.ToInt64(_property.Value);
        var flagValue = Convert.ToInt64(enumValue);
        
        return (currentValue & flagValue) == flagValue;
    }

    public void UpdateValue()
    {
        var enumType = _property.PropertyType;
        long result = 0;
        
        foreach (var enumValue in _enumValues)
        {
            if (enumValue.IsSelected)
            {
                result |= Convert.ToInt64(enumValue.Value);
            }
        }
        
        _property.Value = Enum.ToObject(enumType, result);
        OnPropertyChanged(nameof(EnumValues));
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class EnumValueViewModel : INotifyPropertyChanged
{
    private readonly EnumFlagsPropertyViewModel _parent;
    private readonly string _displayName;
    private readonly object _value;
    private bool _isSelected;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public EnumValueViewModel(string displayName, object value, bool isSelected, EnumFlagsPropertyViewModel parent)
    {
        _displayName = displayName;
        _value = value;
        _isSelected = isSelected;
        _parent = parent;
    }

    public string DisplayName => _displayName;
    public object Value => _value;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                _parent.UpdateValue();
            }
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using WPFNode.Commands;
using WPFNode.Interfaces;
using ICommand = System.Windows.Input.ICommand;

namespace WPFNode.ViewModels.PropertyEditors;

public class EnumFlagsPropertyViewModel : INotifyPropertyChanged
{
    private readonly INodeProperty _property;
    private readonly ObservableCollection<EnumValueViewModel> _enumValues = new();
    private string _searchText = string.Empty;
    private bool _isExpanded = true;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public EnumFlagsPropertyViewModel(INodeProperty property)
    {
        _property = property;
        
        SelectAllCommand = new SimpleCommand(SelectAll);
        UnselectAllCommand = new SimpleCommand(UnselectAll);
        
        if (property.PropertyType.IsEnum)
        {
            InitializeEnumValues();
        }
    }

    public ICommand SelectAllCommand { get; }
    public ICommand UnselectAllCommand { get; }
    
    public ObservableCollection<EnumValueViewModel> EnumValues => _enumValues;
    
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }
    }
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                UpdateFilteredItems();
                OnPropertyChanged(nameof(SearchText));
            }
        }
    }
    
    public int TotalItemCount => _enumValues.Count;
    
    private void SelectAll()
    {
        foreach (var value in _enumValues)
        {
            value.IsSelected = true;
        }
        UpdateValue();
    }
    
    private void UnselectAll()
    {
        foreach (var value in _enumValues)
        {
            value.IsSelected = false;
        }
        UpdateValue();
    }
    
    private void UpdateFilteredItems()
    {
        foreach (var value in _enumValues)
        {
            value.IsVisible = string.IsNullOrEmpty(_searchText) || 
                             value.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
        }
        OnPropertyChanged(nameof(EnumValues));
    }

    private void InitializeEnumValues()
    {
        _enumValues.Clear();
        
        var enumType = _property.PropertyType;
        var enumValues = Enum.GetValues(enumType);
        
        foreach (var value in enumValues)
        {
            if (Convert.ToInt64(value) != 0) // 0 값은 일반적으로 'None'이므로 제외
            {
                var enumName = Enum.GetName(enumType, value) ?? value.ToString();
                var isSelected = IsValueSelected(value);
                var enumValueViewModel = new EnumValueViewModel(enumName, value, isSelected, this);
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
    private bool _isVisible = true;
    
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

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

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
    private readonly List<EnumValueViewModel> _enumValues = new();
    private readonly ObservableCollection<EnumFlagsCategoryViewModel> _categories = new();
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
    
    public ObservableCollection<EnumFlagsCategoryViewModel> Categories => _categories;
    
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
        foreach (var category in _categories)
        {
            category.SetAllValues(true);
        }
        
        UpdateValue();
    }
    
    private void UnselectAll()
    {
        foreach (var category in _categories)
        {
            category.SetAllValues(false);
        }
        
        UpdateValue();
    }
    
    private void UpdateFilteredItems()
    {
        // 모든 카테고리에 검색 텍스트 전달
        foreach (var category in _categories)
        {
            category.SearchText = _searchText;
        }
        
        OnPropertyChanged(nameof(Categories));
    }

    private void InitializeEnumValues()
    {
        _enumValues.Clear();
        _categories.Clear();
        
        var enumType = _property.PropertyType;
        var enumValues = Enum.GetValues(enumType);
        var enumCategories = new Dictionary<string, List<EnumValueViewModel>>();
        
        // 기본 카테고리 이름
        const string defaultCategory = "일반";
        
        foreach (var value in enumValues)
        {
            if (Convert.ToInt64(value) != 0) // 0 값은 일반적으로 'None'이므로 제외
            {
                var enumName = Enum.GetName(enumType, value) ?? value.ToString();
                var displayName = enumName;
                
                // 카테고리 결정 (이름 기반 휴리스틱)
                string category = defaultCategory;
                
                // 이름에 밑줄이 있으면 앞부분을 카테고리로 사용
                if (enumName.Contains('_'))
                {
                    var parts = enumName.Split('_', 2);
                    if (parts.Length > 1)
                    {
                        category = parts[0];
                        displayName = parts[1];
                    }
                }
                // 이름이 대문자로 시작하는 케이스들 그룹화
                else if (enumName.Length > 1 && char.IsUpper(enumName[0]))
                {
                    // 두 번째 대문자를 찾아 카테고리 분리
                    for (int i = 1; i < enumName.Length; i++)
                    {
                        if (char.IsUpper(enumName[i]))
                        {
                            category = enumName.Substring(0, i);
                            displayName = enumName.Substring(i);
                            break;
                        }
                    }
                }
                
                var isSelected = IsValueSelected(value);
                var enumValueViewModel = new EnumValueViewModel(displayName, value, isSelected, this);
                _enumValues.Add(enumValueViewModel);
                
                // 카테고리별로 그룹화
                if (!enumCategories.TryGetValue(category, out var categoryValues))
                {
                    categoryValues = new List<EnumValueViewModel>();
                    enumCategories[category] = categoryValues;
                }
                
                categoryValues.Add(enumValueViewModel);
            }
        }
        
        // 카테고리가 하나도 없으면 기본 카테고리 하나만 생성
        if (enumCategories.Count == 0)
        {
            enumCategories[defaultCategory] = new List<EnumValueViewModel>();
        }
        
        // 카테고리별 뷰모델 생성 및 추가
        foreach (var category in enumCategories.OrderBy(c => c.Key != defaultCategory).ThenBy(c => c.Key))
        {
            var categoryViewModel = new EnumFlagsCategoryViewModel(category.Key, category.Value, this);
            _categories.Add(categoryViewModel);
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
        
        // 카테고리 속성 변경 이벤트 발생을 위한 업데이트
        OnPropertyChanged(nameof(Categories));
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

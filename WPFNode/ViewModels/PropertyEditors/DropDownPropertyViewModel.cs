using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using WPFNode.Interfaces;
using WPFNode.Models.Properties;
using WPFNode.ViewModels.Base;

namespace WPFNode.ViewModels.PropertyEditors;

/// <summary>
/// 드롭다운 목록을 제공하는 속성의 ViewModel
/// </summary>
public class DropDownPropertyViewModel : ViewModelBase
{
    private readonly INodeProperty _property;
    private object? _nodeInstance;
    private MethodInfo? _optionsProviderMethod;
    
    private DropDownItemViewModel? _selectedItem;
    
    /// <summary>
    /// 선택된 항목
    /// </summary>
    public DropDownItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                // 선택된 항목이 변경되면 속성 값도 업데이트
                if (value != null)
                {
                    _property.Value = value.Value;
                }
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 드롭다운 옵션 목록
    /// </summary>
    public ObservableCollection<DropDownItemViewModel> Options { get; } = new ObservableCollection<DropDownItemViewModel>();

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="property">드롭다운으로 표시할 속성</param>
    public DropDownPropertyViewModel(INodeProperty property)
    {
        _property = property ?? throw new ArgumentNullException(nameof(property));
        
        // 노드 인스턴스 가져오기
        _nodeInstance = GetNodeInstance(property);
        
        if (_nodeInstance != null)
        {
            // 드롭다운 옵션 찾기
            var dropDownOption = property.Options.FirstOrDefault(o => o.OptionType == "DropDown");
            
            if (dropDownOption != null)
            {
                // 타입에 맞는 DropDownOption 찾기
                FindAndSetupDropDownOption(dropDownOption, property.PropertyType);
            }
        }
        
        // 속성 값이 변경될 때 선택된 항목 업데이트
        if (property is System.ComponentModel.INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += (sender, e) => 
            {
                if (e.PropertyName == nameof(INodeProperty.Value))
                {
                    UpdateSelectedItem();
                }
            };
        }
    }
    
    /// <summary>
    /// 특정 타입에 맞는 DropDownOption을 찾아 설정
    /// </summary>
    private void FindAndSetupDropDownOption(INodePropertyOption option, Type propertyType)
    {
        // 리플렉션을 사용하여 적절한 타입의 GetOptionsMethod, GetDisplayName 메서드 접근
        var optionType = option.GetType();
        
        // OptionsMethodName 속성 가져오기
        var optionsMethodNameProperty = optionType.GetProperty("OptionsMethodName");
        if (optionsMethodNameProperty != null)
        {
            var methodName = optionsMethodNameProperty.GetValue(option) as string;
            if (!string.IsNullOrEmpty(methodName))
            {
                // 옵션 메서드 찾기
                _optionsProviderMethod = _nodeInstance.GetType().GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }
        
        // DisplayNameConverter 속성 검사
        var displayConverterProperty = optionType.GetProperty("DisplayNameConverter");
        var displayConverter = displayConverterProperty?.GetValue(option);
        
        // GetDisplayName 메서드 가져오기
        var getDisplayNameMethod = optionType.GetMethod("GetDisplayName");
        
        // 정적 옵션 목록 가져오기
        var getStaticOptionsMethod = optionType.GetMethod("GetStaticOptions");
        var staticOptions = getStaticOptionsMethod?.Invoke(option, null) as IEnumerable<object>;
        
        if (staticOptions != null)
        {
            // 정적 옵션으로 드롭다운 항목 설정
            foreach (var item in staticOptions)
            {
                string? displayName = item?.ToString();
                
                // GetDisplayName 메서드가 있으면 사용
                if (getDisplayNameMethod != null)
                {
                    try
                    {
                        displayName = getDisplayNameMethod.Invoke(option, new[] { item }) as string;
                    }
                    catch
                    {
                        // 변환 실패 시 기본 ToString 사용
                    }
                }
                
                Options.Add(new DropDownItemViewModel 
                { 
                    Value = item, 
                    DisplayName = displayName ?? string.Empty 
                });
            }
        }
        else if (_optionsProviderMethod != null)
        {
            // 동적 옵션 로드
            LoadDynamicOptions(getDisplayNameMethod, option);
        }
        
        // 현재 값에 맞는 항목 선택
        UpdateSelectedItem();
    }
    
    /// <summary>
    /// 동적 옵션 목록 로드
    /// </summary>
    private void LoadDynamicOptions(MethodInfo? getDisplayNameMethod, INodePropertyOption option)
    {
        if (_optionsProviderMethod == null || _nodeInstance == null) return;
        
        Options.Clear();
        
        try
        {
            // 메서드 호출하여 옵션 목록 가져오기
            var result = _optionsProviderMethod.Invoke(_nodeInstance, null);
            
            if (result is IEnumerable<object> options)
            {
                foreach (var item in options)
                {
                    string? displayName = item?.ToString();
                    
                    // GetDisplayName 메서드가 있으면 사용
                    if (getDisplayNameMethod != null)
                    {
                        try
                        {
                            displayName = getDisplayNameMethod.Invoke(option, new[] { item }) as string;
                        }
                        catch
                        {
                            // 변환 실패 시 기본 ToString 사용
                        }
                    }
                    
                    Options.Add(new DropDownItemViewModel 
                    { 
                        Value = item, 
                        DisplayName = displayName ?? string.Empty 
                    });
                }
            }
        }
        catch (Exception ex)
        {
            // 메서드 호출 오류 처리
            System.Diagnostics.Debug.WriteLine($"옵션 목록 가져오기 오류: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 현재 속성 값에 맞는 항목 선택
    /// </summary>
    private void UpdateSelectedItem()
    {
        var currentValue = _property.Value;
        
        if (currentValue == null)
        {
            SelectedItem = null;
            return;
        }
        
        // 값이 일치하는 항목 찾기
        var matchingItem = Options.FirstOrDefault(item => 
            Object.Equals(item.Value, currentValue));
            
        if (matchingItem != null)
        {
            _selectedItem = matchingItem;
            OnPropertyChanged(nameof(SelectedItem));
        }
        else
        {
            // 일치하는 항목이 없으면 첫 번째 항목 선택
            SelectedItem = Options.FirstOrDefault();
        }
    }
    
    /// <summary>
    /// INodeProperty에서 노드 인스턴스 가져오기
    /// </summary>
    private object? GetNodeInstance(INodeProperty property)
    {
        // Node 속성에 접근 시도
        var nodeProperty = property.GetType().GetProperty("Node", 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        
        if (nodeProperty != null)
        {
            return nodeProperty.GetValue(property);
        }
        
        // _node 필드에 접근 시도
        var nodeField = property.GetType().GetField("_node", 
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        
        if (nodeField != null)
        {
            return nodeField.GetValue(property);
        }
        
        return null;
    }
}

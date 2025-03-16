using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models.Properties;
using WPFNode.ViewModels.Base;

namespace WPFNode.ViewModels.PropertyEditors;

/// <summary>
/// 드롭다운 목록을 제공하는 속성의 ViewModel
/// </summary>
public class DropDownPropertyViewModel : ViewModelBase {
    private readonly INodeProperty          _property;
    private readonly object                 _nodeInstance;
    private readonly NodeDropDownAttribute _dropDownOption;

    private          DropDownItemViewModel? _selectedItem;

    /// <summary>
    /// 선택된 항목
    /// </summary>
    public DropDownItemViewModel? SelectedItem {
        get => _selectedItem;
        set {
            if (_selectedItem != value) {
                _selectedItem = value;
                // 선택된 항목이 변경되면 속성 값도 업데이트
                if (value != null) {
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
    public DropDownPropertyViewModel(INodeProperty property) {
        _property = property ?? throw new ArgumentNullException(nameof(property));

        // 노드 인스턴스 가져오기
        _nodeInstance = property.Node;

        // 노드 타입에서 프로퍼티나 필드 찾기
        var members = _nodeInstance.GetType()
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field);

        // property.Name과 일치하는 멤버 찾기
        var member = members.FirstOrDefault(m => m.Name == property.Name);
        if (member == null) 
            throw new InvalidOperationException($"Member '{property.Name}' not found in type '{_nodeInstance.GetType().Name}'");

        // 멤버에서 NodeDropDownAttribute 조회
        _dropDownOption = member.GetCustomAttribute<NodeDropDownAttribute>()!;
        if (_dropDownOption == null)
            throw new InvalidOperationException($"NodeDropDownAttribute not found on member '{property.Name}'");

        // 타입에 맞는 DropDownOption 찾기
        FindAndSetupDropDownOption(_dropDownOption, property.PropertyType);

        // 속성 값이 변경될 때 선택된 항목 업데이트
        if (property is System.ComponentModel.INotifyPropertyChanged notifyPropertyChanged) {
            notifyPropertyChanged.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof(INodeProperty.Value)) {
                    UpdateSelectedItem();
                }
            };
        }
    }

    /// <summary>
    /// 특정 타입에 맞는 DropDownOption을 찾아 설정
    /// </summary>
    private void FindAndSetupDropDownOption(NodeDropDownAttribute option, Type propertyType) {
        // OptionsMethodName 속성 가져오기
        // 옵션 메서드 찾기
        var optionsProviderMethod =
            _nodeInstance.GetType()
                         .GetMethod(
                             option.ElementsMethodName,
                             BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // GetDisplayName 메서드 가져오기
        var nameConverterMethod =
            !string.IsNullOrEmpty(option.NameConverterMethodName)
                ? _nodeInstance.GetType()
                               .GetMethod(
                                   option.NameConverterMethodName,
                                   BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                               )
                : null;

        if (optionsProviderMethod?.Invoke(_nodeInstance, null) is IEnumerable<object> staticOptions) {
            // 정적 옵션으로 드롭다운 항목 설정
            foreach (var item in staticOptions) {
                string? displayName = item.ToString();

                // GetDisplayName 메서드가 있으면 사용
                if (nameConverterMethod != null) {
                    try {
                        displayName = nameConverterMethod.Invoke(_nodeInstance, new[] { item }) as string;
                    }
                    catch {
                        // 변환 실패 시 기본 ToString 사용
                    }
                }

                Options.Add(new() {
                                Value       = item,
                                DisplayName = displayName ?? string.Empty
                            });
            }
        }

        // 현재 값에 맞는 항목 선택
        UpdateSelectedItem();
    }

    /// <summary>
    /// 현재 속성 값에 맞는 항목 선택
    /// </summary>
    private void UpdateSelectedItem() {
        var currentValue = _property.Value;

        if (currentValue == null) {
            SelectedItem = null;
            return;
        }

        // 값이 일치하는 항목 찾기
        var matchingItem = Options.FirstOrDefault(item =>
                                                      Object.Equals(item.Value, currentValue));

        if (matchingItem != null) {
            _selectedItem = matchingItem;
            OnPropertyChanged(nameof(SelectedItem));
        }
        else {
            // 일치하는 항목이 없으면 첫 번째 항목 선택
            SelectedItem = Options.FirstOrDefault();
        }
    }
}
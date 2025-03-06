using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using WPFNode.Commands;
using WPFNode.Interfaces;

namespace WPFNode.ViewModels.PropertyEditors;

public class ListPropertyViewModel : INotifyPropertyChanged
{
    private readonly INodeProperty _property;
    private readonly ObservableCollection<ListItemViewModel> _items = new();
    private readonly Type _elementType;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public ListPropertyViewModel(INodeProperty property)
    {
        _property = property;
        
        // 리스트의 요소 타입 결정
        _elementType = GetElementType(property.PropertyType);
        
        // 명령 초기화
        AddItemCommand = new SimpleCommand(AddItem);
        ClearItemsCommand = new SimpleCommand(ClearItems);
        
        // 기존 항목 로드
        LoadItems();
    }

    private Type GetElementType(Type listType)
    {
        // 제네릭 리스트인 경우
        if (listType.IsGenericType && 
            (listType.GetGenericTypeDefinition() == typeof(List<>) ||
             listType.GetGenericTypeDefinition() == typeof(IList<>) ||
             listType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
             listType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            return listType.GetGenericArguments()[0];
        }
        
        // 배열인 경우
        if (listType.IsArray)
        {
            return listType.GetElementType() ?? typeof(object);
        }
        
        // 기본값
        return typeof(object);
    }

    public ObservableCollection<ListItemViewModel> Items => _items;
    
    public int ItemCount => _items.Count;
    
    public System.Windows.Input.ICommand AddItemCommand { get; }
    public System.Windows.Input.ICommand ClearItemsCommand { get; }

    private void LoadItems()
    {
        _items.Clear();
        
        if (_property.Value is IEnumerable<object> collection)
        {
            int index = 0;
            foreach (var item in collection)
            {
                _items.Add(new ListItemViewModel(item, index++, this));
            }
        }
        
        OnPropertyChanged(nameof(ItemCount));
    }

    private void AddItem()
    {
        // 기본값 생성
        object? defaultValue = _elementType.IsValueType ? 
            Activator.CreateInstance(_elementType) : 
            null;
        
        // 항목 추가
        _items.Add(new ListItemViewModel(defaultValue, _items.Count, this));
        
        // 리스트 업데이트
        UpdateList();
        
        OnPropertyChanged(nameof(ItemCount));
    }

    private void ClearItems()
    {
        _items.Clear();
        UpdateList();
        OnPropertyChanged(nameof(ItemCount));
    }

    public void RemoveItem(ListItemViewModel item)
    {
        _items.Remove(item);
        
        // 인덱스 재조정
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].Index = i;
        }
        
        UpdateList();
        OnPropertyChanged(nameof(ItemCount));
    }

    public void MoveItemUp(ListItemViewModel item)
    {
        int index = _items.IndexOf(item);
        if (index > 0)
        {
            _items.Move(index, index - 1);
            
            // 인덱스 재조정
            _items[index].Index = index;
            _items[index - 1].Index = index - 1;
            
            UpdateList();
        }
    }

    private void UpdateList()
    {
        // 현재 항목들로 새 리스트 생성
        var listType = typeof(List<>).MakeGenericType(_elementType);
        var list = Activator.CreateInstance(listType);
        
        // Add 메서드 호출
        var addMethod = listType.GetMethod("Add");
        if (addMethod != null)
        {
            foreach (var item in _items)
            {
                addMethod.Invoke(list, new[] { item.Value });
            }
        }
        
        // 속성 업데이트
        _property.Value = list;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ListItemViewModel : INotifyPropertyChanged
{
    private readonly ListPropertyViewModel _parent;
    private object? _value;
    private int _index;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public ListItemViewModel(object? value, int index, ListPropertyViewModel parent)
    {
        _value = value;
        _index = index;
        _parent = parent;
        
        RemoveCommand = new SimpleCommand(() => _parent.RemoveItem(this));
        MoveUpCommand = new SimpleCommand(() => _parent.MoveItemUp(this));
    }

    public object? Value
    {
        get => _value;
        set
        {
            if (!Equals(_value, value))
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public int Index
    {
        get => _index;
        set
        {
            if (_index != value)
            {
                _index = value;
                OnPropertyChanged(nameof(Index));
            }
        }
    }

    public object? Editor => CreateEditor();

    private object? CreateEditor()
    {
        // 여기서는 간단한 텍스트 편집기만 반환
        // 실제 구현에서는 타입에 맞는 편집기를 생성해야 함
        return Value?.ToString() ?? string.Empty;
    }

    public System.Windows.Input.ICommand RemoveCommand { get; }
    public System.Windows.Input.ICommand MoveUpCommand { get; }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 
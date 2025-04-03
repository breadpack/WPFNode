using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFNode.Interfaces;
using WPFNode.Controls;
using System;
using System.Globalization;
using System.ComponentModel;

namespace WPFNode.Controls.PropertyControls;

// Type 변환을 위한 컨버터 클래스
public class TypeValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value as Type;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}

public class TypeSelectorControlProvider : IPropertyControlProvider
{
    public bool CanHandle(INodeProperty property) => property.PropertyType == typeof(Type);
    
    public FrameworkElement CreateControl(INodeProperty property)
    {
        var typeSelector = new TypeSelector();
        
        // NodeProperty 클래스의 명확한 Type 값을 바인딩하기 위한 특수 래퍼 생성
        var propertyWrapper = new TypePropertyWrapper(property);
        
        var binding = new Binding("TypeValue")
        {
            Source = propertyWrapper,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        
        typeSelector.SetBinding(TypeSelector.SelectedTypeProperty, binding);
        
        // 래퍼 객체를 태그에 저장하여 GC에 의해 회수되지 않도록 함
        typeSelector.Tag = propertyWrapper;
        
        return typeSelector;
    }
    
    public int Priority => 0;
    public string ControlTypeId => "TypeSelector";
}

// NodeProperty 바인딩을 위한 래퍼 클래스
public class TypePropertyWrapper : INotifyPropertyChanged
{
    private readonly INodeProperty _property;
    
    public TypePropertyWrapper(INodeProperty property)
    {
        _property = property;
        // 원본 프로퍼티의 변경 감지
        _property.PropertyChanged += (s, e) => {
            if (e.PropertyName == "Value")
            {
                OnPropertyChanged(nameof(TypeValue));
            }
        };
    }
    
    public Type TypeValue
    {
        get => _property.Value as Type;
        set
        {
            if (_property.Value != value)
            {
                _property.Value = value;
                OnPropertyChanged(nameof(TypeValue));
            }
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 
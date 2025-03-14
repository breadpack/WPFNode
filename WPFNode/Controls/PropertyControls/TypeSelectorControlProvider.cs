using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFNode.Interfaces;
using WPFNode.Controls;

namespace WPFNode.Controls.PropertyControls;

public class TypeSelectorControlProvider : IPropertyControlProvider
{
    public bool CanHandle(INodeProperty property) => property.PropertyType == typeof(Type);
    
    public FrameworkElement CreateControl(INodeProperty property)
    {
        var typeSelector = new TypeSelector();
        
        var binding = new Binding("Value")
        {
            Source = property,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        typeSelector.SetBinding(TypeSelector.SelectedTypeProperty, binding);
        
        return typeSelector;
    }
    
    public int Priority => 0;
    public string ControlTypeId => "TypeSelector";
} 
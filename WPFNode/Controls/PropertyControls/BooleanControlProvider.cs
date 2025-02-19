using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFNode.Interfaces;

namespace WPFNode.Controls.PropertyControls;

public class BooleanControlProvider : IPropertyControlProvider
{
    public bool CanHandle(Type propertyType) => propertyType == typeof(bool);
    
    public FrameworkElement CreateControl(INodeProperty property)
    {
        var checkBox = new CheckBox();
        var binding = new Binding("Value")
        {
            Source              = property,
            Mode                = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
        return checkBox;
    }
    
    public int    Priority      => 0;
    public string ControlTypeId => "Boolean";
}
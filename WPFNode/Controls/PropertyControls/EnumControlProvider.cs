using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFNode.Interfaces;

namespace WPFNode.Controls.PropertyControls;

public class EnumControlProvider : IPropertyControlProvider
{
    public bool CanHandle(Type propertyType) => propertyType.IsEnum;
    
    public FrameworkElement CreateControl(INodeProperty property)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = Enum.GetValues(property.PropertyType)
        };
        
        var binding = new Binding("Value")
        {
            Source              = property,
            Mode                = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        comboBox.SetBinding(ComboBox.SelectedValueProperty, binding);
        
        return comboBox;
    }
    
    public int    Priority      => 0;
    public string ControlTypeId => "Enum";
}
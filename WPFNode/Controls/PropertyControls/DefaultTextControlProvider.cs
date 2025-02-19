using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFNode.Interfaces;

namespace WPFNode.Controls.PropertyControls;

public class DefaultTextControlProvider : IPropertyControlProvider
{
    public bool CanHandle(Type propertyType) => true;  // 모든 타입 처리 가능
    
    public FrameworkElement CreateControl(INodeProperty property)
    {
        var textBox = new TextBox();
        var binding = new Binding("Value")
        {
            Source = property,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        textBox.SetBinding(TextBox.TextProperty, binding);
        return textBox;
    }
    
    public int Priority => -1;  // 가장 낮은 우선순위
    public string ControlTypeId => "Default";
} 
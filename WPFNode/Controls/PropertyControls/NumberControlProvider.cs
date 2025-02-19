using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFNode.Interfaces;

namespace WPFNode.Controls.PropertyControls;

public class NumberControlProvider : IPropertyControlProvider
{
    private static readonly HashSet<Type> SupportedTypes = new()
    {
        typeof(int),
        typeof(double),
        typeof(float),
        typeof(decimal)
    };
    
    public bool CanHandle(Type propertyType) => SupportedTypes.Contains(propertyType);
    
    public FrameworkElement CreateControl(INodeProperty property)
    {
        var textBox = new TextBox();
        var binding = new Binding("Value")
        {
            Source              = property,
            Mode                = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        textBox.SetBinding(TextBox.TextProperty, binding);
        
        // 숫자만 입력 가능하도록 이벤트 처리
        textBox.PreviewTextInput += (s, e) =>
        {
            if (!char.IsDigit(e.Text[0]) && e.Text[0] != '.' && e.Text[0] != '-')
            {
                e.Handled = true;
            }
        };
        
        return textBox;
    }
    
    public int    Priority      => 0;
    public string ControlTypeId => "Number";
}
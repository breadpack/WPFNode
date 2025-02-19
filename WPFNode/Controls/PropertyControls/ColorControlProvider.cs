using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFNode.Interfaces;

namespace WPFNode.Controls.PropertyControls;

public class ColorControlProvider : IPropertyControlProvider
{
    public bool CanHandle(Type propertyType) => propertyType == typeof(Color);
    
    public FrameworkElement CreateControl(INodeProperty property)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = new[]
            {
                Colors.White,
                Colors.Black,
                Colors.Red,
                Colors.Green,
                Colors.Blue,
                Colors.Yellow,
                Colors.Orange,
                Colors.Purple
            }
        };

        comboBox.ItemTemplate = CreateColorItemTemplate();
        
        var binding = new Binding("Value")
        {
            Source              = property,
            Mode                = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        comboBox.SetBinding(ComboBox.SelectedValueProperty, binding);
        
        return comboBox;
    }
    
    private DataTemplate CreateColorItemTemplate()
    {
        var template   = new DataTemplate();
        var stackPanel = new FrameworkElementFactory(typeof(StackPanel));
        stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        
        var rectangle = new FrameworkElementFactory(typeof(Rectangle));
        rectangle.SetValue(FrameworkElement.WidthProperty, 16.0);
        rectangle.SetValue(FrameworkElement.HeightProperty, 16.0);
        rectangle.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 4, 0));
        
        var fillBinding = new Binding(".");
        rectangle.SetValue(Shape.FillProperty, new SolidColorBrush(Colors.Black));
        rectangle.SetBinding(Shape.FillProperty, fillBinding);
        
        var textBlock = new FrameworkElementFactory(typeof(TextBlock));
        textBlock.SetBinding(TextBlock.TextProperty, new Binding());
        
        stackPanel.AppendChild(rectangle);
        stackPanel.AppendChild(textBlock);
        
        template.VisualTree = stackPanel;
        return template;
    }
    
    public int    Priority      => 0;
    public string ControlTypeId => "Color";
}
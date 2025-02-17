using System.Windows;

namespace WPFNode.Controls;

public class InputPortControl : PortControl
{
    static InputPortControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(InputPortControl),
                                                 new FrameworkPropertyMetadata(typeof(InputPortControl)));
    }

    public InputPortControl()
    {
        IsInput = true;
    }
}
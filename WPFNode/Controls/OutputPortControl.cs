using System.Windows;

namespace WPFNode.Controls;

public class OutputPortControl : PortControl
{
    static OutputPortControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(OutputPortControl),
                                                 new FrameworkPropertyMetadata(typeof(OutputPortControl)));
    }

    public OutputPortControl()
    {
        IsInput = false;
    }
}
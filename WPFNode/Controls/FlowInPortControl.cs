using System.Windows;

namespace WPFNode.Controls;

public class FlowInPortControl : PortControl
{
    static FlowInPortControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FlowInPortControl),
                                               new FrameworkPropertyMetadata(typeof(FlowInPortControl)));
    }

    public FlowInPortControl()
    {
        IsInput = true;
    }
}

using System.Windows;

namespace WPFNode.Controls;

public class FlowOutPortControl : PortControl
{
    static FlowOutPortControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FlowOutPortControl),
                                                new FrameworkPropertyMetadata(typeof(FlowOutPortControl)));
    }

    public FlowOutPortControl()
    {
        IsInput = false;
    }
}

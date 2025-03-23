using System.Windows;
using System.Windows.Controls;
using WPFNode.Models;

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

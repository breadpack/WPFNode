using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPFNode.Core.ViewModels.Nodes;

namespace WPFNode.Controls;

public class PortControl : Control
{
    static PortControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(PortControl),
            new FrameworkPropertyMetadata(typeof(PortControl)));
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(NodePortViewModel),
            typeof(PortControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public NodePortViewModel? ViewModel
    {
        get => (NodePortViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PortControl control)
        {
            control.DataContext = e.NewValue;
        }
    }

    public PortControl()
    {
        Background = Brushes.LightGray;
        BorderBrush = Brushes.DarkGray;
    }
} 

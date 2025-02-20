using System.Windows;
using System.Windows.Controls;
using WPFNode.Services;
using WPFNode.Interfaces;

namespace WPFNode.Controls;

public partial class TypeSelector : UserControl
{
    private readonly bool _pluginOnly;

    public static readonly DependencyProperty SelectedTypeProperty =
        DependencyProperty.Register(
            nameof(SelectedType),
            typeof(Type),
            typeof(TypeSelector),
            new FrameworkPropertyMetadata(null, 
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public Type? SelectedType
    {
        get => (Type?)GetValue(SelectedTypeProperty);
        set => SetValue(SelectedTypeProperty, value);
    }

    public TypeSelector(bool pluginOnly = false)
    {
        _pluginOnly = pluginOnly;
        InitializeComponent();
    }

    private void OnSelectTypeClick(object sender, RoutedEventArgs e)
    {
        var dialog = new TypeSelectorDialog(_pluginOnly)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedType = dialog.SelectedType;
        }
    }
} 
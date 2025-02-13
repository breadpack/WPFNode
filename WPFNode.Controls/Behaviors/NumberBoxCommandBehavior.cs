using System.Windows;
using System.Windows.Input;
using WPFNode.Controls.Commands;
using WPFNode.Core.ViewModels.Nodes;

namespace WPFNode.Controls.Behaviors;

public interface INumberInputNode
{
    void Increment();
    void Decrement();
}

public static class NumberBoxCommandBehavior
{
    public static readonly DependencyProperty CommandBindingsProperty =
        DependencyProperty.RegisterAttached(
            "CommandBindings",
            typeof(CommandBindingCollection),
            typeof(NumberBoxCommandBehavior),
            new PropertyMetadata(null, OnCommandBindingsChanged));

    public static CommandBindingCollection GetCommandBindings(DependencyObject obj)
    {
        return (CommandBindingCollection)obj.GetValue(CommandBindingsProperty);
    }

    public static void SetCommandBindings(DependencyObject obj, CommandBindingCollection value)
    {
        obj.SetValue(CommandBindingsProperty, value);
    }

    private static void OnCommandBindingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            var bindings = new CommandBindingCollection
            {
                new CommandBinding(NumberBoxCommands.Increment, OnIncrement, OnCanExecuteNumberCommand),
                new CommandBinding(NumberBoxCommands.Decrement, OnDecrement, OnCanExecuteNumberCommand)
            };

            element.CommandBindings.Clear();
            foreach (CommandBinding binding in bindings)
            {
                element.CommandBindings.Add(binding);
            }
        }
    }

    private static void OnCanExecuteNumberCommand(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Parameter is NodeViewModel viewModel)
        {
            e.CanExecute = viewModel.Model is INumberInputNode;
        }
    }

    private static void OnIncrement(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is NodeViewModel viewModel && 
            viewModel.Model is INumberInputNode node)
        {
            node.Increment();
        }
    }

    private static void OnDecrement(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is NodeViewModel viewModel && 
            viewModel.Model is INumberInputNode node)
        {
            node.Decrement();
        }
    }
} 
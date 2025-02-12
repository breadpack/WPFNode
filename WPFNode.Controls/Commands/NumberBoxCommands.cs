using System.Windows.Input;

namespace WPFNode.Controls.Commands;

public static class NumberBoxCommands
{
    public static readonly RoutedCommand Increment = new(
        nameof(Increment),
        typeof(NumberBoxCommands));

    public static readonly RoutedCommand Decrement = new(
        nameof(Decrement),
        typeof(NumberBoxCommands));
} 
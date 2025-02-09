using System.Windows;
using System.Windows.Media;

namespace WPFNode.Controls;

public static class ControlExtensions
{
    public static T? GetParentOfType<T>(this DependencyObject control) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(control);
        while (parent != null && !(parent is T))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        return parent as T;
    }

    public static IEnumerable<DependencyObject> GetVisualDescendants(this DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in GetVisualDescendants(child))
            {
                yield return descendant;
            }
        }
    }
} 
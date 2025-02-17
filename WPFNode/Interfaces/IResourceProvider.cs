using System.Windows;

namespace WPFNode.Interfaces;

public interface IResourceProvider
{
    DataTemplate? GetEditorTemplate(string resourceKey);
} 
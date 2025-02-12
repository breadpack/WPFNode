using System.Windows;

namespace WPFNode.Core.Interfaces;

public interface IResourceProvider
{
    DataTemplate? GetEditorTemplate(string resourceKey);
} 
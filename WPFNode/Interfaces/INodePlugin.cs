using System.Windows;

namespace WPFNode.Interfaces;

public interface INodePlugin
{
    IEnumerable<ResourceDictionary> GetNodeStyles();
} 
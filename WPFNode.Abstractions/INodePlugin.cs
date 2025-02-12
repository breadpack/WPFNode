using System.Collections.Generic;
using System.Windows;

namespace WPFNode.Abstractions;

public interface INodePlugin
{
    IEnumerable<ResourceDictionary> GetNodeStyles();
} 
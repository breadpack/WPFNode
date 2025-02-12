using System.Collections.Generic;
using System.Windows;

namespace WPFNode.Core.Interfaces;

public interface INodePlugin
{
    IEnumerable<ResourceDictionary> GetNodeStyles();
} 
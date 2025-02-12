using System;
using System.Collections.Generic;
using System.Windows;
using WPFNode.Abstractions;

namespace WPFNode.Plugins.Basic;

public class BasicNodePlugin : INodePlugin
{
    public IEnumerable<ResourceDictionary> GetNodeStyles()
    {
        yield return new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/WPFNode.Plugins.Basic;component/Themes/InputNodes.xaml")
        };
    }
} 
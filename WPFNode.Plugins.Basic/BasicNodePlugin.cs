using System;
using System.Collections.Generic;
using System.Windows;
using WPFNode.Controls.PropertyControls;
using WPFNode.Interfaces;
using WPFNode.Plugins.Basic.Nodes;

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
    
    public IEnumerable<IPropertyControlProvider> GetPropertyControlProviders()
    {
        // 기본 PropertyControlProvider 인스턴스 반환
        yield return new GuidControlProvider();
        yield return new EnumFlagsControlProvider();
        yield return new ListControlProvider();
    }
    
    // Type 변환 및 값 편집 관련 노드들을 등록
    static BasicNodePlugin()
    {
        // 노드 타입 등록
        RegisterNodeTypes();
    }
    
    private static void RegisterNodeTypes()
    {
        // 여기서는 자동으로 리플렉션으로 등록되므로 코드를 비워둡니다.
        // NodePluginService에서 WPFNode.Plugins.Basic 네임스페이스의 노드들을 자동으로 검색합니다.
    }
}

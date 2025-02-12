using System;
using System.Collections.Generic;
using WPFNode.Abstractions;
using WPFNode.Core.Models;

namespace WPFNode.Core.Interfaces;

public interface INodePluginService
{
    IReadOnlyCollection<Type> NodeTypes { get; }
    void LoadPlugins(string pluginPath);
    void RegisterNodeType(Type nodeType);
    INode CreateNode(Type nodeType);
    IEnumerable<string> GetCategories();
    IEnumerable<Type> GetNodeTypesByCategory(string category);
    
    // 메타데이터 관련 메서드 추가
    NodeMetadata GetNodeMetadata(Type nodeType);
    IEnumerable<NodeMetadata> GetNodeMetadataByCategory(string category);
    IEnumerable<NodeMetadata> GetAllNodeMetadata();
} 
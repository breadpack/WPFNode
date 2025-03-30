using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WPFNode.Interfaces;

public interface INodeCanvas
{
    IReadOnlyList<INode> Nodes { get; }
    IReadOnlyList<IConnection> Connections { get; }
    
    // Node 생성 및 관리
    T     CreateNode<T>(double x = 0,    double y = 0) where T : INode;
    INode CreateNode(Type      nodeType, double x = 0, double y = 0);
    void  RemoveNode(INode     node);
    T?    Q<T>(string          id) where T : INode;
    
    // 연결 관리
    IConnection Connect(IOutputPort    source, IInputPort target);
    void        Disconnect(IConnection connection);
    
    // 실행
    Task               ExecuteAsync(CancellationToken cancellationToken = default);
    T? Q<T>() where T : INode;
} 
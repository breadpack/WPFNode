using System;
using System.Collections.Generic;
using WPFNode.Interfaces;
using WPFNode.Interfaces.Flow;

namespace WPFNode.Models.Flow;

/// <summary>
/// 흐름 포트의 기본 추상 구현입니다.
/// </summary>
public abstract class FlowPort : IFlowPort
{
    private readonly List<IFlowConnection> _connections = new();
    private readonly INode _node;
    private readonly string _name;

    /// <summary>
    /// 흐름 포트 생성자
    /// </summary>
    /// <param name="node">포트가 소속된 노드</param>
    /// <param name="name">포트 이름</param>
    protected FlowPort(INode node, string name)
    {
        _node = node ?? throw new ArgumentNullException(nameof(node));
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <inheritdoc />
    public INode Node => _node;

    /// <inheritdoc />
    public abstract FlowPortType PortType { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<IFlowConnection> Connections => _connections.AsReadOnly();

    /// <summary>
    /// 포트 이름
    /// </summary>
    public string Name => _name;

    /// <inheritdoc />
    public virtual void AddConnection(IFlowConnection connection)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (!_connections.Contains(connection))
        {
            _connections.Add(connection);
        }
    }

    /// <inheritdoc />
    public virtual void RemoveConnection(IFlowConnection connection)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        _connections.Remove(connection);
    }
}

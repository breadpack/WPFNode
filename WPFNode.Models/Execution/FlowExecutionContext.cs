using Microsoft.Extensions.Logging;
using WPFNode.Interfaces;

namespace WPFNode.Models.Execution;

/// <summary>
/// Flow 기반 노드 실행을 위한 컨텍스트 클래스
/// </summary>
public class FlowExecutionContext : IExecutionContext
{
    private readonly Dictionary<INode, NodeExecutionState> _nodeStates = new();
    private readonly Dictionary<INode, Dictionary<IOutputPort, object?>> _nodeOutputs = new();
    private readonly ILogger? _logger;
    
    /// <summary>
    /// 현재 활성화된 FlowInPort
    /// </summary>
    public IFlowInPort? ActiveFlowInPort { get; private set; }
    
    /// <summary>
    /// 활성화된 FlowInPort를 설정합니다.
    /// </summary>
    public void SetActiveFlowInPort(IFlowInPort port)
    {
        ActiveFlowInPort = port;
    }

    public FlowExecutionContext(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 노드가 이미 실행되었는지 확인합니다.
    /// </summary>
    public bool IsNodeExecuted(INode node)
    {
        return GetNodeExecutionState(node).IsExecuted;
    }

    /// <summary>
    /// 노드를 실행완료로 표시합니다.
    /// </summary>
    public void MarkNodeExecuted(INode node)
    {
        var state = GetNodeExecutionState(node);
        state.IsExecuted = true;
        _logger?.LogDebug("Node {NodeType} marked as executed", 
            node.GetType().Name);
    }
    
    /// <summary>
    /// 노드의 실행 상태를 가져옵니다. 없으면 새로 생성합니다.
    /// </summary>
    public NodeExecutionState GetNodeExecutionState(INode node)
    {
        if (!_nodeStates.TryGetValue(node, out var state))
        {
            state = new NodeExecutionState();
            _nodeStates[node] = state;
        }
        return state;
    }
    
    /// <summary>
    /// 노드의 출력 포트 값을 설정합니다.
    /// </summary>
    public void SetNodeOutput(INode node, IOutputPort port, object? value)
    {
        if (!_nodeOutputs.TryGetValue(node, out var outputs))
        {
            outputs = new Dictionary<IOutputPort, object?>();
            _nodeOutputs[node] = outputs;
        }

        outputs[port] = value;
        _logger?.LogDebug("Node {NodeType} output set for port {PortName}: {Value}", 
            node.GetType().Name, port.Name, value);
    }

    /// <summary>
    /// 입력 포트에 연결된 출력 값을 가져옵니다.
    /// </summary>
    public T? GetInputValue<T>(IInputPort port)
    {
        if (port == null) return default;
        
        // 입력 포트에 연결된 연결 가져오기
        var connection = port.Connections.FirstOrDefault();
        if (connection == null) return default;
        
        // 연결의 소스 포트와 노드 가져오기
        var sourcePort = connection.Source as IOutputPort;
        var sourceNode = sourcePort?.Node;
        
        if (sourcePort == null || sourceNode == null) return default;
        
        // 소스 노드의 출력 값 가져오기
        if (_nodeOutputs.TryGetValue(sourceNode, out var outputs) && 
            outputs.TryGetValue(sourcePort, out var value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }
            
            try
            {
                // 타입 변환 시도
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                _logger?.LogWarning("Cannot convert output value from {SourceType} to {TargetType}", 
                    value?.GetType().Name ?? "null", typeof(T).Name);
                return default;
            }
        }

        return default;
    }

    /// <summary>
    /// 실행 상태를 초기화합니다.
    /// </summary>
    public void Reset()
    {
        _nodeStates.Clear();
        _nodeOutputs.Clear();
    }
}

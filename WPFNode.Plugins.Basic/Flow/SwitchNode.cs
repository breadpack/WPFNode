using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Plugins.Basic.Flow;

/// <summary>
/// 입력 값에 따라 실행 경로를 분기하는 Switch 노드입니다.
/// </summary>
[NodeCategory("Flow Control")]
[NodeName("Switch")]
[NodeDescription("입력 값에 따라 실행 경로를 분기합니다.")]
public class SwitchNode : DynamicNode
{
    private readonly Dictionary<string, FlowOutPort> _casePorts = new();
    
    /// <summary>
    /// 스위치 값 (입력)
    /// </summary>
    [NodeInput("Value")]
    public InputPort<object> InputValue { get; private set; }
    
    /// <summary>
    /// Switch 노드 진입 Flow 포트
    /// </summary>
    [NodeFlowIn("Enter")]
    public FlowInPort FlowIn { get; private set; }
    
    /// <summary>
    /// 어떤 케이스도 일치하지 않을 때 실행 Flow 포트
    /// </summary>
    [NodeFlowOut("Default")]
    public FlowOutPort DefaultPort { get; private set; }

    // 기존 NodeBase에서는 protected로 정의되어 있는 Logger를 여기서는 새로 정의
    private ILogger? _logger;
    protected ILogger? Logger => _logger;
    
    public SwitchNode(INodeCanvas canvas, Guid id, ILogger? logger = null) 
        : base(canvas, id)
    {
        _logger = logger;
        
        // 생성자에서 기본 케이스 포트 추가
        AddCasePort("0");
        AddCasePort("1");
        AddCasePort("2");
    }
    
    /// <summary>
    /// 새로운 케이스 포트 추가
    /// </summary>
    public FlowOutPort AddCasePort(string caseValue)
    {
        if (_casePorts.TryGetValue(caseValue, out var existingPort))
        {
            return existingPort;
        }
        
        var casePort = AddFlowOutPort($"Case: {caseValue}");
        _casePorts[caseValue] = casePort;
        return casePort;
    }
    
    /// <summary>
    /// 케이스 포트 제거
    /// </summary>
    public void RemoveCasePort(string caseValue)
    {
        if (_casePorts.TryGetValue(caseValue, out var port))
        {
            RemoveFlowOutPort(port);
            _casePorts.Remove(caseValue);
        }
    }
    
    /// <summary>
    /// 노드의 처리 로직을 구현합니다.
    /// </summary>
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(CancellationToken cancellationToken = default) {
        // 입력 값 가져오기
        var inputValue = InputValue.GetValueOrDefault()?.ToString() ?? string.Empty;
        
        Logger?.LogDebug("SwitchNode: Input value = '{Value}'", inputValue);
        
        // 일치하는 케이스 찾기
        if (_casePorts.TryGetValue(inputValue, out var matchedPort))
        {
            Logger?.LogDebug("SwitchNode: Matched case '{Case}'", inputValue);
            yield return matchedPort;
        }
        else
        {
            Logger?.LogDebug("SwitchNode: No matching case, using default");
            yield return DefaultPort;
        }
    }
}

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Plugins.Basic.Flow;

/// <summary>
/// 입력 값에 따라 실행 경로를 분기하는 Switch 노드입니다.
/// 입력 타입과 케이스 수를 설정할 수 있으며, 동적으로 케이스 포트가 생성됩니다.
/// </summary>
[NodeCategory("Flow Control")]
[NodeName("Switch")]
[NodeDescription("입력 값에 따라 실행 경로를 분기합니다.")]
public class SwitchNode : NodeBase {
    // 동적 포트 관리 필드
    private IInputPort _inputValuePort;
    private Dictionary<string, FlowOutPort> _flowOutPorts = new();

    // 기본 속성 정의
    [NodeProperty("입력 타입", CanConnectToPort = false, OnValueChanged = nameof(OnTypeChanged))]
    public NodeProperty<Type> ValueType { get; private set; }

    [NodeProperty("케이스 수", Format = "N0", CanConnectToPort = false, OnValueChanged = nameof(OnCaseCountChanged))]
    public NodeProperty<int> CaseCount { get; private set; }

    // 기본 포트 정의
    [NodeFlowIn("Enter")]
    public FlowInPort FlowIn { get; private set; }

    [NodeFlowOut("Default")]
    public FlowOutPort DefaultPort { get; private set; }

    /// <summary>
    /// Switch 조건으로 사용될 입력 값 포트
    /// </summary>
    public IInputPort InputValue => _inputValuePort;

    /// <summary>
    /// 인덱스를 통해 케이스 프로퍼티에 접근할 수 있는 인덱서
    /// </summary>
    /// <param name="index">케이스 인덱스 (0-based)</param>
    /// <returns>케이스 프로퍼티</returns>
    /// <exception cref="IndexOutOfRangeException">해당 인덱스의 케이스가 존재하지 않는 경우</exception>
    public INodeProperty this[int index] {
        get {
            var prop = Properties.FirstOrDefault(p => p.Name == $"Case_{index}");
            if (prop == null) {
                throw new IndexOutOfRangeException($"Case_{index} not found. Ensure CaseCount is set appropriately.");
            }
            return prop;
        }
    }

    public SwitchNode(INodeCanvas canvas, Guid id)
        : base(canvas, id) { }

    // 속성 변경 핸들러 - 단순화
    private void OnTypeChanged() => ReconfigurePorts();

    private void OnCaseCountChanged() => ReconfigurePorts();

    /// <summary>
    /// 노드의 처리 로직을 구현합니다.
    /// </summary>
    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken) {
        // 입력 값 가져오기
        object inputValue = null;

        if (_inputValuePort != null) {
            try {
                dynamic dynamicPort = _inputValuePort;
                inputValue = dynamicPort.GetValueOrDefault();
            }
            catch {
                Logger?.LogWarning("SwitchNode: Failed to get input value");
            }
        }

        Logger?.LogDebug($"SwitchNode: Input value = '{inputValue}'");

        // 케이스 프로퍼티들을 순회하며 일치하는 값 찾기
        if (inputValue != null) {
            var caseProps = Properties.Where(p => p.Name.StartsWith("Case_"));
            foreach (var prop in caseProps) {
                if (prop.Value?.Equals(inputValue) ?? false) {
                    if (_flowOutPorts.TryGetValue(prop.Name, out var matchedPort)) {
                        Logger?.LogDebug($"SwitchNode: Matched case for value '{inputValue}'");
                        yield return matchedPort;
                        yield break;
                    }
                }
            }
        }

        // 일치하는 케이스를 찾지 못한 경우
        Logger?.LogDebug("SwitchNode: No matching case, using default");
        yield return DefaultPort;
    }

    // 기존 OnConfigureDynamicPorts 대신 빌더 패턴을 사용하는 Configure 메서드를 구현
    protected override void Configure(NodeBuilder builder) {
        // 1. 입력 포트 구성
        var type = ValueType?.Value ?? typeof(object);
        _inputValuePort = builder.Input("Value", type);

        // 2. 케이스 프로퍼티 구성
        var targetCount = Math.Max(1, CaseCount?.Value ?? 3);

        // 필요한 수만큼 케이스 프로퍼티 생성/수정
        for (int i = 0; i < targetCount; i++) {
            // 기존 프로퍼티 사용 또는 새로 생성
            var prop = builder.Property<object>($"Case_{i}", $"케이스 {i}");

            // 비어있으면 기본값 설정
            if (prop.Value == null) {
                try {
                    prop.Value = Convert.ChangeType(i.ToString(), type);
                }
                catch {
                    prop.Value = null;
                }
            }

            // 출력 포트 생성
            if (prop.Value != null) {
                var portName = $"Case {prop.Value}";
                _flowOutPorts[prop.Name] = builder.FlowOut(portName);
            }
        }
    }

    public IFlowOutPort CaseFlowOut(int i) {
        // 케이스 인덱스에 해당하는 FlowOutPort를 반환
        if (_flowOutPorts.TryGetValue($"Case_{i}", out var port)) {
            return port;
        }
        throw new IndexOutOfRangeException($"Case_{i} not found. Ensure CaseCount is set appropriately.");
    }
}
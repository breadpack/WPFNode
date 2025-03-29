using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using WPFNode.Models.Execution;
using Microsoft.Extensions.Logging;

namespace WPFNode.Plugins.Basic.Flow;

[NodeCategory("Flow Control")]
[NodeName("Select")]
[NodeDescription("입력 값에 따라 여러 케이스 중 하나를 선택하여 해당 값을 출력합니다.")]
public class SelectNode : NodeBase {
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; private set; }

    [NodeFlowOut]
    public IFlowOutPort FlowOut { get; private set; }

    [NodeProperty("Case Type", OnValueChanged = nameof(OnTypeChanged))]
    public NodeProperty<Type> CaseType { get; private set; }

    [NodeProperty("Output Type", OnValueChanged = nameof(OnTypeChanged))]
    public NodeProperty<Type> OutputType { get; private set; }

    [NodeProperty("Case Count", OnValueChanged = nameof(OnCaseCountChanged))]
    public NodeProperty<int> CaseCount { get; private set; }

    private IInputPort                                                    _valuePort;
    private Dictionary<object, (INodeProperty Case, INodeProperty Value)> _caseValuePorts = new();
    private IOutputPort                                                   _outputPort;

    public SelectNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Name = "Select";
        Description = "입력 값에 따라 여러 케이스 중 하나를 선택하여 해당 값을 출력합니다.";
    }

    private void OnTypeChanged() => ReconfigurePorts();
    private void OnCaseCountChanged() => ReconfigurePorts();

    protected override void Configure(NodeBuilder builder) {
        ConfigureInputOutputPorts(builder);
        ConfigureCaseProperties(builder);
    }

    private void ConfigureInputOutputPorts(NodeBuilder builder) {
        var caseType = CaseType?.Value ?? typeof(object);
        var outputType = OutputType?.Value ?? typeof(object);
        
        _valuePort = builder.Input("Value", caseType);
        _outputPort = builder.Output("Result", outputType);
    }

    private void ConfigureCaseProperties(NodeBuilder builder) {
        var caseType = CaseType?.Value ?? typeof(object);
        var targetCount = Math.Max(1, CaseCount?.Value ?? 3);

        _caseValuePorts.Clear();

        for (int i = 0; i < targetCount; i++) {
            try {
                var casePortName      = $"Case_{i}";
                var prop      = builder.Property(casePortName, $"Case {i}", caseType);
                var inputPort = builder.Property($"{prop.Name}_Value", $"Case Value {i}", OutputType.Value ?? typeof(object));
                _caseValuePorts[casePortName] = (prop, inputPort);
            }
            catch (Exception ex) {
                Logger?.LogWarning($"SelectNode: Error configuring case {i}: {ex.Message}");
            }
        }
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken) {
        var inputValue = GetInputValue();
        if (inputValue == null) {
            yield return HandleNoMatch();
            yield break;
        }

        var matchResult = FindMatchingCase(inputValue);
        if (matchResult.IsMatch) {
            yield return HandleMatch(matchResult.SelectedValue);
            yield break;
        }

        yield return HandleNoMatch();
    }

    private object? GetInputValue() {
        var type = CaseType?.Value ?? typeof(object);
        object? inputValue = null;

        if (_valuePort != null) {
            try {
                dynamic dynamicPort = _valuePort;
                inputValue = dynamicPort.GetValueOrDefault();
                
                if (inputValue != null && inputValue.GetType() != type) {
                    try {
                        inputValue = Convert.ChangeType(inputValue, type);
                    }
                    catch (Exception ex) {
                        Logger?.LogWarning($"SelectNode: Failed to convert input value to type {type.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex) {
                Logger?.LogWarning($"SelectNode: Failed to get input value: {ex.Message}");
            }
        }

        Logger?.LogDebug($"SelectNode: Processing input value '{inputValue}' of type {type.Name}");
        return inputValue;
    }

    private (bool IsMatch, object? SelectedValue) FindMatchingCase(object inputValue) {
        var type      = CaseType?.Value ?? typeof(object);

        foreach (var prop in _caseValuePorts.Values) {
            try {
                if (prop.Case.Value?.Equals(inputValue) ?? false) {
                    Logger?.LogDebug($"SelectNode: Matched case '{prop.Case.Name}' with value '{prop.Case.Value}'");
                    return (true, prop.Value.Value);
                }
            }
            catch (Exception ex) {
                Logger?.LogWarning($"SelectNode: Error processing case {prop.Case.Name}: {ex.Message}");
            }
        }

        return (false, null);
    }

    private IFlowOutPort HandleMatch(object selectedValue) {
        try {
            _outputPort.Value = selectedValue;
        }
        catch (Exception ex) {
            Logger?.LogWarning($"SelectNode: Failed to set output value: {ex.Message}");
        }
        return FlowOut;
    }

    private IFlowOutPort HandleNoMatch() {
        Logger?.LogDebug("SelectNode: No matching case found");
        try {
            _outputPort.Value = null;
        }
        catch (Exception ex) {
            Logger?.LogWarning($"SelectNode: Failed to set output value to null: {ex.Message}");
        }
        return FlowOut;
    }
}
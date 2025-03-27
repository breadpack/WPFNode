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
public class SelectNode : DynamicNode {
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

    private IInputPort _valuePort;
    private Dictionary<object, IInputPort> _caseValuePorts = new();
    private IOutputPort _outputPort;

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
                _caseValuePorts[casePortName] = (IInputPort)inputPort;
            }
            catch (Exception ex) {
                Logger?.LogWarning($"SelectNode: Error configuring case {i}: {ex.Message}");
            }
        }
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken) {
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

    private (bool IsMatch, object SelectedValue) FindMatchingCase(object inputValue) {
        var type = CaseType?.Value ?? typeof(object);
        var caseProps = GetOrderedCaseProperties();

        foreach (var prop in caseProps) {
            try {
                var caseValue = GetConvertedCaseValue(prop, type);
                if (caseValue?.Equals(inputValue) ?? false) {
                    if (_caseValuePorts.TryGetValue($"{prop.Name}_Value", out var matchedPort)) {
                        Logger?.LogDebug($"SelectNode: Matched case '{prop.Name}' with value '{caseValue}'");
                        return (true, GetCaseValue(matchedPort));
                    }
                }
            }
            catch (Exception ex) {
                Logger?.LogWarning($"SelectNode: Error processing case {prop.Name}: {ex.Message}");
            }
        }

        return (false, null);
    }

    private IEnumerable<INodeProperty> GetOrderedCaseProperties() {
        return Properties.Where(p => p.Name.StartsWith("Case_"))
                        .OrderBy(p => {
                            string indexStr = p.Name.Substring("Case_".Length);
                            return int.TryParse(indexStr, out int index) ? index : int.MaxValue;
                        });
    }

    private object GetConvertedCaseValue(INodeProperty prop, Type type) {
        var caseValue = prop.Value;
        if (caseValue != null && caseValue.GetType() != type) {
            caseValue = Convert.ChangeType(caseValue, type);
        }
        return caseValue;
    }

    private object GetCaseValue(IInputPort matchedPort) {
        dynamic dynamicCasePort = matchedPort;
        return dynamicCasePort.GetValueOrDefault();
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

    public IInputPort CaseInput(int i) {
        var prop = Properties.FirstOrDefault(p => p.Name == $"Case_{i}");
        if (prop == null) {
            throw new IndexOutOfRangeException($"Case_{i} not found. Ensure CaseCount is set appropriately.");
        }
        
        if (_caseValuePorts.TryGetValue($"{prop.Name}_Value", out var port)) {
            return port;
        }
        
        throw new InvalidOperationException($"Input port for case {i} not found.");
    }
}
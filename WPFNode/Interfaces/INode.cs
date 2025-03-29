using System.ComponentModel;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WPFNode.Interfaces;

public interface INode : INotifyPropertyChanged, IJsonSerializable
{
    Guid Guid { get; }
    string Id { get; }
    string Name { get; set; }
    string Category { get; }
    string Description { get; }
    double X { get; set; }
    double Y { get; set; }
    
    IReadOnlyList<IInputPort>    InputPorts  { get; }
    IReadOnlyList<IOutputPort>   OutputPorts { get; }
    IReadOnlyList<IFlowInPort>   FlowInPorts { get; }
    IReadOnlyList<IFlowOutPort>  FlowOutPorts { get; }
    IReadOnlyList<INodeProperty> Properties  { get; }
    
    bool CanExecuteCommand(string commandName, object? parameter = null);
    void ExecuteCommand(string commandName, object? parameter = null);

    IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        Models.Execution.FlowExecutionContext? context,
        CancellationToken                      cancellationToken = default);
}

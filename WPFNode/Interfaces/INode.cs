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
    
    Task ExecuteAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 노드를 실행하고 활성화할 FlowOutPort를 yield return으로 순차적으로 반환합니다.
    /// </summary>
    IAsyncEnumerable<IFlowOutPort> ExecuteAsyncFlow([EnumeratorCancellation] CancellationToken cancellationToken = default);
    bool CanExecuteCommand(string commandName, object? parameter = null);
    void ExecuteCommand(string commandName, object? parameter = null);
}

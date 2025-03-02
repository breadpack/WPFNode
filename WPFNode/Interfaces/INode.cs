using System.ComponentModel;
using System.Text.Json;
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
    
    IReadOnlyList<IInputPort> InputPorts { get; }
    IReadOnlyList<IOutputPort> OutputPorts { get; }
    IReadOnlyDictionary<string, INodeProperty> Properties { get; }
    
    Task ExecuteAsync(CancellationToken cancellationToken = default);
    bool CanExecuteCommand(string commandName, object? parameter = null);
    void ExecuteCommand(string commandName, object? parameter = null);
} 
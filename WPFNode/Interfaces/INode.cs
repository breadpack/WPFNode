using System.ComponentModel;
using System.Text.Json;

namespace WPFNode.Interfaces;

public interface INode : INotifyPropertyChanged, IJsonSerializable
{
    Guid Id { get; }
    string Name { get; set; }
    string Category { get; }
    string Description { get; }
    double X { get; set; }
    double Y { get; set; }
    
    IReadOnlyList<IInputPort> InputPorts { get; }
    IReadOnlyList<IOutputPort> OutputPorts { get; }
    IReadOnlyDictionary<string, INodeProperty> Properties { get; }
    
    Task ProcessAsync();
    bool CanExecuteCommand(string commandName, object? parameter = null);
    void ExecuteCommand(string commandName, object? parameter = null);
    void Initialize();
} 
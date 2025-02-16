using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace WPFNode.Abstractions;

public interface INode : INotifyPropertyChanged
{
    Guid Id { get; }
    string Name { get; set; }
    string Category { get; }
    string Description { get; set; }
    double X { get; set; }
    double Y { get; set; }
    bool IsProcessing { get; }
    bool IsOutputNode { get; }
    bool IsInitialized { get; }
    
    IReadOnlyList<IInputPort> InputPorts { get; }
    IReadOnlyList<IOutputPort> OutputPorts { get; }
    
    IReadOnlyDictionary<string, INodeProperty> Properties { get; }
    
    Task ProcessAsync();
    bool CanExecuteCommand(string commandName, object? parameter = null);
    void ExecuteCommand(string commandName, object? parameter = null);
    void Initialize();
} 
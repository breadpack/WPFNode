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
    string Description { get; }
    double X { get; set; }
    double Y { get; set; }
    bool IsProcessing { get; }
    
    IReadOnlyList<IPort> InputPorts { get; }
    IReadOnlyList<IPort> OutputPorts { get; }
    
    Task ProcessAsync();
    bool CanExecuteCommand(string commandName, object? parameter = null);
    void ExecuteCommand(string commandName, object? parameter = null);
    void Initialize();
} 
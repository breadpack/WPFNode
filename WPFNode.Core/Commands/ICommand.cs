namespace WPFNode.Core.Commands;

public interface ICommand
{
    void Execute();
    void Undo();
    string Description { get; }
} 

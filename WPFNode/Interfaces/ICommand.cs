namespace WPFNode.Interfaces;

public interface ICommand
{
    void Execute();
    void Undo();
    string Description { get; }
} 

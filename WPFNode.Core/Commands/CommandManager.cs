using System.Collections.ObjectModel;

namespace WPFNode.Core.Commands;

public class CommandManager
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();
    private bool _isExecuting;

    public event EventHandler? CanUndoChanged;
    public event EventHandler? CanRedoChanged;
    public event EventHandler<string>? CommandExecuted;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Execute(ICommand command)
    {
        if (_isExecuting) return;

        _isExecuting = true;
        try
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();

            CanUndoChanged?.Invoke(this, EventArgs.Empty);
            CanRedoChanged?.Invoke(this, EventArgs.Empty);
            CommandExecuted?.Invoke(this, command.Description);
        }
        finally
        {
            _isExecuting = false;
        }
    }

    public void Undo()
    {
        if (!CanUndo || _isExecuting) return;

        _isExecuting = true;
        try
        {
            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);

            CanUndoChanged?.Invoke(this, EventArgs.Empty);
            CanRedoChanged?.Invoke(this, EventArgs.Empty);
            CommandExecuted?.Invoke(this, $"실행 취소: {command.Description}");
        }
        finally
        {
            _isExecuting = false;
        }
    }

    public void Redo()
    {
        if (!CanRedo || _isExecuting) return;

        _isExecuting = true;
        try
        {
            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);

            CanUndoChanged?.Invoke(this, EventArgs.Empty);
            CanRedoChanged?.Invoke(this, EventArgs.Empty);
            CommandExecuted?.Invoke(this, $"다시 실행: {command.Description}");
        }
        finally
        {
            _isExecuting = false;
        }
    }
} 

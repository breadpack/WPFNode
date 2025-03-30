using System;
using System.Collections.Generic;
using System.Linq;
using WPFNode.Commands;
using WPFNode.Interfaces;

namespace WPFNode.Services;

public class NodeCommandService : INodeCommandService
{
    private readonly Stack<WPFNode.Interfaces.ICommand> _undoStack = new();
    private readonly Stack<WPFNode.Interfaces.ICommand> _redoStack = new();
    private readonly INodeModelService _modelService;
    private INodeCanvas? _canvas;
    private readonly Dictionary<Guid, INode> _nodes = new();
    private bool _isExecuting;

    public event EventHandler? CanUndoChanged;
    public event EventHandler? CanRedoChanged;
    public event EventHandler<string>? CommandExecuted;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public NodeCommandService(INodeModelService modelService)
    {
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
    }

    public void SetCanvas(INodeCanvas canvas)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
    }

    public bool ExecuteCommand(Guid nodeId, string commandName, object? parameter = null)
    {
        var node = FindNodeById(nodeId);
        if (node == null || !node.CanExecuteCommand(commandName, parameter))
            return false;

        try
        {
            node.ExecuteCommand(commandName, parameter);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool CanExecuteCommand(Guid nodeId, string commandName, object? parameter = null)
    {
        var node = FindNodeById(nodeId);
        return node != null && node.CanExecuteCommand(commandName, parameter);
    }

    public void Execute(WPFNode.Interfaces.ICommand command)
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

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        CanUndoChanged?.Invoke(this, EventArgs.Empty);
        CanRedoChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ExecuteNodeCommand(Guid nodeId, string commandName, object? parameter = null)
    {
        var node = FindNodeById(nodeId);
        if (node != null && node.CanExecuteCommand(commandName, parameter))
        {
            var nodeCommand = new NodeCommand(node, commandName, parameter);
            Execute(nodeCommand);
        }
    }

    // NodeCanvas에서 노드를 찾는 도우미 메서드
    private INode? FindNodeById(Guid nodeId)
    {
        // Canvas가 설정되어 있으면 Canvas에서 노드를 찾음
        if (_canvas != null)
            return _canvas.Nodes.FirstOrDefault(node => node.Guid == nodeId);
        
        // Canvas가 없으면 내부 Dictionary에서 찾음
        return _nodes.TryGetValue(nodeId, out var node) ? node : null;
    }
}

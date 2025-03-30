using System;

namespace WPFNode.Interfaces;

public interface INodeCommandService
{
    // 기존 노드 명령 실행 메서드 (노드 등록/해제 기능 제거)
    bool ExecuteCommand(Guid nodeId, string commandName, object? parameter = null);
    bool CanExecuteCommand(Guid nodeId, string commandName, object? parameter = null);
    
    // CommandManager에서 가져온 Undo/Redo 기능
    void Execute(ICommand command);
    void Undo();
    void Redo();
    void Clear();
    bool CanUndo { get; }
    bool CanRedo { get; }
    
    // 이벤트
    event EventHandler? CanUndoChanged;
    event EventHandler? CanRedoChanged;
    event EventHandler<string>? CommandExecuted;
    
    // 새로 추가: 노드 명령을 Command 패턴으로 래핑하여 실행
    void ExecuteNodeCommand(Guid nodeId, string commandName, object? parameter = null);
}

namespace WPFNode.Interfaces;

/// <summary>
/// 반복 실행이 필요한 노드를 위한 인터페이스입니다.
/// </summary>
public interface ILoopNode : INode
{
    /// <summary>
    /// 반복 실행이 완료되었는지 여부를 가져옵니다.
    /// </summary>
    bool IsLoopCompleted { get; }

    /// <summary>
    /// 노드의 상태를 초기화합니다.
    /// </summary>
    void Reset();

    Task<bool> ShouldContinueAsync(CancellationToken cancellationToken = default);
} 
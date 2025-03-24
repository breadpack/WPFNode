namespace WPFNode.Models.Execution;

/// <summary>
/// 노드의 실행 상태를 추적하는 클래스입니다.
/// </summary>
public class NodeExecutionState
{
    /// <summary>
    /// 실패 상태를 나타내는 정적 인스턴스
    /// </summary>
    public static readonly NodeExecutionState Failed = new NodeExecutionState { IsFailed = true };

    /// <summary>
    /// 노드가 실행되었는지 여부
    /// </summary>
    public bool IsExecuted { get; set; }
    
    /// <summary>
    /// 노드 실행이 실패했는지 여부
    /// </summary>
    public bool IsFailed { get; private set; }
    
    /// <summary>
    /// 노드의 실행 횟수
    /// </summary>
    public int ExecutionCount { get; private set; }
    
    /// <summary>
    /// 마지막 실행 시간
    /// </summary>
    public DateTime LastExecutionTime { get; private set; }
    
    /// <summary>
    /// 루프 내에서의 반복 횟수 (루프 노드에만 적용)
    /// </summary>
    public int LoopIteration { get; set; }
    
    /// <summary>
    /// 노드의 재실행 플래그 (루프백 처리를 위한 상태)
    /// </summary>
    public bool ShouldReExecute { get; set; }
    
    /// <summary>
    /// 노드가 실행되었음을 표시
    /// </summary>
    public void MarkExecuted()
    {
        IsExecuted = true;
        ExecutionCount++;
        LastExecutionTime = DateTime.Now;
    }
    
    /// <summary>
    /// 노드의 실행 상태를 초기화
    /// </summary>
    public void Reset()
    {
        IsExecuted = false;
    }
    
    /// <summary>
    /// 루프 반복 횟수를 증가
    /// </summary>
    public void IncrementLoopIteration()
    {
        LoopIteration++;
    }
    
    /// <summary>
    /// 루프 상태를 초기화
    /// </summary>
    public void ResetLoopState()
    {
        LoopIteration = 0;
    }
}

namespace WPFNode.Interfaces;

/// <summary>
/// 상태를 초기화할 수 있는 노드를 위한 인터페이스입니다.
/// </summary>
public interface IResettable
{
    /// <summary>
    /// 노드의 상태를 초기화합니다.
    /// </summary>
    void Reset();
} 
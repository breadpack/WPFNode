using WPFNode.Interfaces;

namespace WPFNode.Interfaces.Flow;

/// <summary>
/// 흐름 포트의 타입을 지정합니다.
/// </summary>
public enum FlowPortType
{
    /// <summary>
    /// 흐름 입력 포트
    /// </summary>
    FlowIn,
    
    /// <summary>
    /// 흐름 출력 포트
    /// </summary>
    FlowOut
}

/// <summary>
/// 실행 흐름을 제어하는 포트에 대한 기본 인터페이스입니다.
/// </summary>
public interface IFlowPort
{
    /// <summary>
    /// 포트가 소속된 노드
    /// </summary>
    INode Node { get; }
    
    /// <summary>
    /// 흐름 포트의 타입
    /// </summary>
    FlowPortType PortType { get; }
    
    /// <summary>
    /// 이 포트에 연결된 흐름 연결 목록
    /// </summary>
    IReadOnlyCollection<IFlowConnection> Connections { get; }
    
    /// <summary>
    /// 흐름 연결 추가
    /// </summary>
    /// <param name="connection">추가할 흐름 연결</param>
    void AddConnection(IFlowConnection connection);
    
    /// <summary>
    /// 흐름 연결 제거
    /// </summary>
    /// <param name="connection">제거할 흐름 연결</param>
    void RemoveConnection(IFlowConnection connection);
}

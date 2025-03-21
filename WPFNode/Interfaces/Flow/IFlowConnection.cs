using System;

namespace WPFNode.Interfaces.Flow;

/// <summary>
/// 흐름 포트 간의 연결을 나타내는 인터페이스입니다.
/// </summary>
public interface IFlowConnection
{
    /// <summary>
    /// 연결의 고유 식별자
    /// </summary>
    Guid Guid { get; }
    
    /// <summary>
    /// 소스 흐름 출력 포트
    /// </summary>
    IFlowOutPort Source { get; }
    
    /// <summary>
    /// 타겟 흐름 입력 포트
    /// </summary>
    IFlowInPort Target { get; }
}

using System;

namespace WPFNode.Interfaces;

/// <summary>
/// 실행 흐름을 제어하는 Flow 포트를 정의합니다.
/// </summary>
public interface IFlowNodePort : INodePort
{
    /// <summary>
    /// 포트를 실행합니다.
    /// </summary>
    void Execute();
} 
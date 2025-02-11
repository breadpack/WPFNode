using System;

namespace WPFNode.Abstractions;

public interface IConnection
{
    Guid Id { get; }
    IPort Source { get; }
    IPort Target { get; }
    
    // 추가적인 확장 가능성을 위한 속성들
    bool IsEnabled { get; set; }
    bool IsValid { get; }
} 
namespace WPFNode.Attributes;

/// <summary>
/// 흐름 출력 포트를 정의하는 어트리뷰트입니다.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class FlowOutPortAttribute : Attribute 
{
    /// <summary>
    /// 포트의 표시 이름
    /// </summary>
    public string DisplayName { get; }
    
    /// <summary>
    /// 포트의 설명
    /// </summary>
    public string? Description { get; }
    
    /// <summary>
    /// 기본 생성자
    /// </summary>
    /// <param name="displayName">표시 이름 (생략 시 프로퍼티 이름 사용)</param>
    /// <param name="description">설명 (선택사항)</param>
    public FlowOutPortAttribute(string? displayName = null, string? description = null) 
    {
        DisplayName = displayName ?? string.Empty;
        Description = description;
    }
}

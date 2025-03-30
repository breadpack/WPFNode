using System;

namespace WPFNode.Interfaces;

/// <summary>
/// 선택 가능한 항목을 나타내는 인터페이스입니다.
/// 노드, 연결선, 그룹 등 캔버스에서 선택 가능한 모든 항목에 적용할 수 있습니다.
/// </summary>
public interface ISelectable
{
    /// <summary>
    /// 항목의 고유 식별자입니다.
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// 항목의 표시 이름입니다.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 항목의 유형을 나타냅니다. (예: "Node", "Connection", "Group" 등)
    /// </summary>
    string SelectionType { get; }
    
    /// <summary>
    /// 항목이 선택되었는지 여부를 나타냅니다.
    /// </summary>
    bool IsSelected { get; }
    
    /// <summary>
    /// 항목을 선택합니다.
    /// </summary>
    /// <param name="clearOthers">다른 항목의 선택을 해제할지 여부</param>
    void Select(bool clearOthers = true);
    
    /// <summary>
    /// 항목의 선택을 해제합니다.
    /// </summary>
    void Deselect();
} 
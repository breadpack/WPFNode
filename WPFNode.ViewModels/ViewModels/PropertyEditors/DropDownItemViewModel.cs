using System;

namespace WPFNode.ViewModels.PropertyEditors
{
    /// <summary>
    /// 드롭다운 목록의 아이템을 표현하는 ViewModel
    /// </summary>
    public class DropDownItemViewModel
    {
        /// <summary>
        /// 아이템의 원본 값
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// 표시될 이름
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// 표시 이름을 문자열로 반환
        /// </summary>
        public override string ToString() => DisplayName;
    }
}

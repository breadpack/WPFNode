using System;

namespace WPFNode.Interfaces
{
    /// <summary>
    /// 노드 속성에 부가적인 옵션을 제공하기 위한 인터페이스
    /// </summary>
    public interface INodePropertyOption
    {
        /// <summary>
        /// 옵션의 유형 식별자
        /// </summary>
        string OptionType { get; }
    }
}

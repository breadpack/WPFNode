using System;
using System.Collections.Generic;
using System.Linq;
using WPFNode.Interfaces;

namespace WPFNode.Models.Properties
{
    /// <summary>
    /// 드롭다운 목록을 제공하는 노드 속성 옵션
    /// </summary>
    public class DropDownOption<T> : INodePropertyOption
    {
        /// <summary>
        /// 옵션 타입 식별자
        /// </summary>
        public string OptionType => "DropDown";

        /// <summary>
        /// 옵션 목록을 제공하는 메서드 이름
        /// </summary>
        public string OptionsMethodName { get; }
        
        /// <summary>
        /// 옵션 목록을 제공하는 메서드의 반환 타입
        /// </summary>
        public string NameConverterMethodName { get; }

        /// <summary>
        /// 동적 옵션을 제공하는 메서드를 지정하여 생성
        /// </summary>
        /// <param name="optionsMethodName">노드 클래스에 구현된 옵션 제공 메서드 이름</param>
        /// <param name="nameConverterMethodName"></param>
        public DropDownOption(string optionsMethodName, string nameConverterMethodName = "")
        {
            OptionsMethodName = optionsMethodName;
            NameConverterMethodName = nameConverterMethodName;
        }
    }
}

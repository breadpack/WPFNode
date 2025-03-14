using System;
using System.Collections.Generic;
using WPFNode.Interfaces;

namespace WPFNode.Models.Properties
{
    /// <summary>
    /// NodeProperty에 대한 확장 메서드를 제공합니다.
    /// </summary>
    public static class NodePropertyExtensions
    {
        /// <summary>
        /// NodeProperty에 옵션을 추가합니다.
        /// </summary>
        /// <typeparam name="T">속성의 타입</typeparam>
        /// <param name="property">옵션을 추가할 속성</param>
        /// <param name="option">추가할 옵션</param>
        /// <returns>옵션이 추가된 속성</returns>
        public static NodeProperty<T> WithOption<T>(this NodeProperty<T> property, INodePropertyOption option)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            
            if (option == null)
                throw new ArgumentNullException(nameof(option));
            
            // 리플렉션을 통해 _options 필드에 직접 접근
            var optionsField = typeof(NodeProperty<T>).GetField("_options", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (optionsField != null && optionsField.GetValue(property) is List<INodePropertyOption> options)
            {
                options.Add(option);
            }
            
            return property;
        }
    }
}

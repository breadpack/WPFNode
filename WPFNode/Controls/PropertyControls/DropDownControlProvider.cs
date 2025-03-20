using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.ViewModels.PropertyEditors;

namespace WPFNode.Controls.PropertyControls {
    public class DropDownControlProvider : IPropertyControlProvider {
        public bool CanHandle(INodeProperty property) {
            // Node 인스턴스의 모든 프로퍼티와 필드를 조회
            var members = property.Node.GetType()
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field);

            // property.Name과 일치하는 멤버 찾기
            var member = members.FirstOrDefault(m => m.Name == property.Name);
            if (member == null) return false;

            // 멤버에서 NodeDropDownAttribute 조회
            var nodeDropDownAttribute = member.GetCustomAttribute<NodeDropDownAttribute>();
            if (nodeDropDownAttribute == null) return false;

            if (string.IsNullOrEmpty(nodeDropDownAttribute.ElementsMethodName))
                return false;

            // ElementsMethodName에 해당하는 메서드가 존재하는지 확인
            var methodInfo = property.Node.GetType().GetMethod(nodeDropDownAttribute.ElementsMethodName, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null) return false;
            
            var returnType = methodInfo.ReturnType;
            return typeof(System.Collections.IEnumerable).IsAssignableFrom(returnType);
        }

        public FrameworkElement CreateControl(INodeProperty property) {
            // ViewModel 생성 (내부적으로 옵션 처리)
            var viewModel = new DropDownPropertyViewModel(property);

            var contentControl = new ContentControl {
                ContentTemplate = Application.Current.TryFindResource("DropDownTemplate") as DataTemplate,
                Content         = viewModel
            };

            return contentControl;
        }

        public int Priority => 110;

        public string ControlTypeId => "DropDown";
    }
}
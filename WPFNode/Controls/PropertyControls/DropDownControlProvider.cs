using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WPFNode.Interfaces;
using WPFNode.ViewModels.PropertyEditors;

namespace WPFNode.Controls.PropertyControls
{
    public class DropDownControlProvider : IPropertyControlProvider
    {
        public bool CanHandle(INodeProperty property)
        {
            return property.Options.Any(o => o.OptionType == "DropDown");
        }

        public FrameworkElement CreateControl(INodeProperty property)
        {
            // ViewModel 생성 (내부적으로 옵션 처리)
            var viewModel = new DropDownPropertyViewModel(property);
            
            var contentControl = new ContentControl
            {
                ContentTemplate = Application.Current.TryFindResource("DropDownTemplate") as DataTemplate,
                Content = viewModel
            };
            
            return contentControl;
        }

        // EnumFlags(100)보다 낮고 List(90)보다 높은 우선순위
        public int Priority => 95;
        
        public string ControlTypeId => "DropDown";
    }
}

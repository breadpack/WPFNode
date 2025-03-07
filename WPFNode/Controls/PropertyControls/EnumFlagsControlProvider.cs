using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFNode.Interfaces;
using WPFNode.ViewModels.PropertyEditors;

namespace WPFNode.Controls.PropertyControls
{
    public class EnumFlagsControlProvider : IPropertyControlProvider
    {
        public bool CanHandle(Type propertyType)
        {
            return propertyType.IsEnum && propertyType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
        }

        public FrameworkElement CreateControl(INodeProperty property)
        {
            var viewModel = new EnumFlagsPropertyViewModel(property);
            
            var contentControl = new ContentControl
            {
                ContentTemplate = Application.Current.TryFindResource("EnumFlagsTemplate") as DataTemplate,
                Content = viewModel
            };
            
            return contentControl;
        }

        public int Priority => 100;
        
        public string ControlTypeId => "EnumFlags";
    }
}

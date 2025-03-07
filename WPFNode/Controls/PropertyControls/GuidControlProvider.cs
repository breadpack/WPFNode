using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFNode.Interfaces;
using WPFNode.ViewModels.PropertyEditors;

namespace WPFNode.Controls.PropertyControls
{
    public class GuidControlProvider : IPropertyControlProvider
    {
        public bool CanHandle(Type propertyType)
        {
            return propertyType == typeof(Guid);
        }

        public FrameworkElement CreateControl(INodeProperty property)
        {
            var viewModel = new GuidPropertyViewModel(property);
            
            var contentControl = new ContentControl
            {
                ContentTemplate = Application.Current.TryFindResource("GuidTemplate") as DataTemplate,
                Content = viewModel
            };
            
            return contentControl;
        }

        public int Priority => 100;
        
        public string ControlTypeId => "Guid";
    }
}

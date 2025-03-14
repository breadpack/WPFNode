using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFNode.Interfaces;
using WPFNode.ViewModels.PropertyEditors;

namespace WPFNode.Controls.PropertyControls
{
    public class ListControlProvider : IPropertyControlProvider
    {
        public bool CanHandle(INodeProperty property) {
            var propertyType = property.PropertyType;
            return (propertyType.IsGenericType && 
                    (propertyType.GetGenericTypeDefinition() == typeof(List<>) ||
                     propertyType.GetGenericTypeDefinition() == typeof(IList<>) ||
                     propertyType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                     propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>))) ||
                   propertyType.IsArray ||
                   typeof(IList).IsAssignableFrom(propertyType);
        }

        public FrameworkElement CreateControl(INodeProperty property)
        {
            var viewModel = new ListPropertyViewModel(property);
            
            var contentControl = new ContentControl
            {
                ContentTemplate = Application.Current.TryFindResource("ListTemplate") as DataTemplate,
                Content = viewModel
            };
            
            return contentControl;
        }

        public int Priority => 90;
        
        public string ControlTypeId => "List";
    }
}

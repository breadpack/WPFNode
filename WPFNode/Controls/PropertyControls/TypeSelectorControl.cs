using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Reflection;
using WPFNode.Interfaces;

namespace WPFNode.Controls.PropertyControls;

public class TypeSelectorControlProvider : IPropertyControlProvider
{
    public bool CanHandle(Type propertyType) => propertyType == typeof(Type);
    
    public FrameworkElement CreateControl(INodeProperty property)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        // 검색 박스
        var searchBox = new TextBox
        {
            Margin = new Thickness(0, 0, 0, 5),
            ToolTip = "타입 검색..."
        };
        
        // 콤보박스
        var comboBox = new ComboBox();
        
        // 타입 목록 로드
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    return Array.Empty<Type>();
                }
            })
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsInterface)
            .OrderBy(t => t.FullName)
            .ToList();

        var view = new ListCollectionView(types);
        view.Filter = item =>
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text)) return true;
            if (item is not Type type) return false;
            
            return type.FullName?.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase) == true ||
                   type.Name.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase);
        };
        
        comboBox.ItemsSource = view;
        comboBox.DisplayMemberPath = "FullName";
        comboBox.IsEditable = true;
        comboBox.IsTextSearchEnabled = true;
        comboBox.StaysOpenOnEdit = true;
        
        // 검색 기능
        searchBox.TextChanged += (s, e) => view.Refresh();
        
        // 바인딩
        var binding = new Binding("Value")
        {
            Source = property,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        comboBox.SetBinding(ComboBox.SelectedItemProperty, binding);
        
        // 그리드에 추가
        Grid.SetRow(searchBox, 0);
        Grid.SetRow(comboBox, 1);
        grid.Children.Add(searchBox);
        grid.Children.Add(comboBox);
        
        return grid;
    }
    
    public int Priority => 0;
    public string ControlTypeId => "TypeSelector";
} 
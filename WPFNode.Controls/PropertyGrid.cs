using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Reflection;
using System.Linq;
using WPFNode.Abstractions.Attributes;
using WPFNode.Abstractions.Constants;
using WPFNode.Core.ViewModels.Nodes;
using WPFNode.Abstractions;
using WPFNode.Core.Models.Properties;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;

namespace WPFNode.Controls;

public class PropertyGrid : Control, INotifyPropertyChanged
{
    private string _searchText = string.Empty;
    private NodeViewModel? _selectedNode;
    private ListCollectionView? _filteredProperties;
    private NodeCanvasViewModel? _canvasViewModel;

    public event PropertyChangedEventHandler? PropertyChanged;

    static PropertyGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGrid),
            new FrameworkPropertyMetadata(typeof(PropertyGrid)));
    }

    public PropertyGrid()
    {
        UpdateProperties();
    }

    public static readonly DependencyProperty CanvasViewModelProperty =
        DependencyProperty.Register(
            nameof(CanvasViewModel),
            typeof(NodeCanvasViewModel),
            typeof(PropertyGrid),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.None,
                OnCanvasViewModelChanged));

    public NodeCanvasViewModel? CanvasViewModel
    {
        get => (NodeCanvasViewModel?)GetValue(CanvasViewModelProperty);
        set => SetValue(CanvasViewModelProperty, value);
    }

    private static void OnCanvasViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyGrid propertyGrid)
        {
            propertyGrid._canvasViewModel = e.NewValue as NodeCanvasViewModel;
            propertyGrid.UpdateProperties();
        }
    }

    public static readonly DependencyProperty SelectedNodeProperty =
        DependencyProperty.Register(
            nameof(SelectedNode),
            typeof(NodeViewModel),
            typeof(PropertyGrid),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedNodeChanged));

    public NodeViewModel? SelectedNode
    {
        get => (NodeViewModel?)GetValue(SelectedNodeProperty);
        set
        {
            SetValue(SelectedNodeProperty, value);
            OnPropertyChanged(nameof(SelectedNode));
            UpdateProperties();
        }
    }

    private static void OnSelectedNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyGrid propertyGrid)
        {
            propertyGrid._selectedNode = e.NewValue as NodeViewModel;
            propertyGrid.UpdateProperties();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                _filteredProperties?.Refresh();
            }
        }
    }

    public ICollectionView? FilteredProperties => _filteredProperties;

    private void UpdateProperties()
    {
        var properties = SelectedNode?.Model?.Properties.Values
            .Select(p => new NodePropertyViewModel(p))
            .ToList() ?? new List<NodePropertyViewModel>();

        _filteredProperties = new ListCollectionView(properties)
        {
            Filter = OnFilterProperties
        };

        OnPropertyChanged(nameof(FilteredProperties));
    }

    private bool OnFilterProperties(object item)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        if (item is NodePropertyViewModel propertyItem)
        {
            return propertyItem.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == DataContextProperty)
        {
            UpdatePropertyControls();
        }
    }

    private void UpdatePropertyControls()
    {
        if (Template?.FindName("PART_PropertyPanel", this) is StackPanel propertyPanel)
        {
            propertyPanel.Children.Clear();

            if (DataContext is INodeProperty property)
            {
                var control = CreatePropertyControl(property);
                if (control != null)
                {
                    propertyPanel.Children.Add(control);
                }
            }
        }
    }

    private FrameworkElement? CreatePropertyControl(INodeProperty property)
    {
        var container = new DockPanel();

        // 기본 컨트롤 생성
        var mainControl = CreateMainControl(property);
        if (mainControl != null)
        {
            // Port 연결 가능한 경우 포트 컨트롤 추가
            if (property.CanConnectToPort && property is IInputPort inputPort && CanvasViewModel != null)
            {
                var portControl = new InputPortControl
                {
                    Style = TryFindResource("PropertyGridInputPortStyle") as Style,
                    ToolTip = $"입력 포트: {property.DisplayName}\n타입: {property.PropertyType.Name}"
                };

                // 포트 뷰모델 설정
                var portViewModel = new NodePortViewModel(inputPort, CanvasViewModel);
                portControl.ViewModel = portViewModel;

                // 포트 상태 변경 감지
                if (property is INotifyPropertyChanged notifyPropertyChanged)
                {
                    notifyPropertyChanged.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(INodeProperty.Value))
                        {
                            portControl.ToolTip = $"입력 포트: {property.DisplayName}\n" +
                                               $"타입: {property.PropertyType.Name}\n" +
                                               $"값: {property.Value}";
                        }
                    };
                }

                // 포트를 오른쪽에 배치
                DockPanel.SetDock(portControl, Dock.Right);
                container.Children.Add(portControl);

                // 포트가 있는 경우 메인 컨트롤의 마진 조정
                mainControl.Margin = new Thickness(0, 0, 4, 0);
            }

            container.Children.Add(mainControl);
            return container;
        }

        return null;
    }

    private FrameworkElement? CreateMainControl(INodeProperty property)
    {
        // 1. 컬렉션 타입 처리
        if (property.ElementType != null)
        {
            return CreateCollectionControl(property);
        }

        // 2. 복합 타입 처리 (사용자 정의 타입)
        if (IsComplexType(property.PropertyType))
        {
            return CreateComplexTypeControl(property);
        }

        // 3. 커스텀 템플릿 검색
        var template = TryFindTemplateForType(property.PropertyType);
        if (template != null)
        {
            return new ContentPresenter
            {
                Content = property,
                ContentTemplate = template
            };
        }

        // 4. 기본 컨트롤 타입에 따른 컨트롤 생성
        return property.ControlType switch
        {
            NodePropertyControlType.TextBox => CreateTextBox(property),
            NodePropertyControlType.NumberBox => CreateNumberBox(property),
            NodePropertyControlType.CheckBox => CreateCheckBox(property),
            NodePropertyControlType.ColorPicker => CreateColorPicker(property),
            NodePropertyControlType.ComboBox => CreateComboBox(property),
            NodePropertyControlType.MultilineText => CreateMultilineTextBox(property),
            _ => CreateDefaultControl(property)
        };
    }

    private bool IsComplexType(Type type)
    {
        return !type.IsPrimitive && 
               type != typeof(string) && 
               type != typeof(decimal) &&
               !type.IsEnum &&
               type.Namespace?.StartsWith("System") != true;
    }

    private FrameworkElement CreateCollectionControl(INodeProperty property)
    {
        var itemsControl = new ItemsControl();
        
        // 컬렉션 아이템 템플릿 설정
        if (property.ElementType != null)
        {
            var itemTemplate = TryFindTemplateForType(property.ElementType) ?? 
                             CreateDefaultItemTemplate(property.ElementType);
            itemsControl.ItemTemplate = itemTemplate;
        }

        // 컬렉션 바인딩
        var binding = new Binding("Value")
        {
            Source = property,
            Mode = BindingMode.TwoWay
        };
        itemsControl.SetBinding(ItemsControl.ItemsSourceProperty, binding);

        return new ScrollViewer
        {
            Content = itemsControl,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 200
        };
    }

    private FrameworkElement CreateComplexTypeControl(INodeProperty property)
    {
        var expander = new Expander
        {
            Header = property.DisplayName,
            IsExpanded = false
        };

        var stackPanel = new StackPanel();
        var control = CreateDefaultControl(property);
        if (control != null)
        {
            stackPanel.Children.Add(control);
        }

        expander.Content = stackPanel;
        return expander;
    }

    private DataTemplate CreateDefaultItemTemplate(Type elementType)
    {
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetBinding(TextBlock.TextProperty, new Binding());
        template.VisualTree = factory;
        return template;
    }

    private DataTemplate? TryFindTemplateForType(Type propertyType)
    {
        // 1. 타입 이름 기반 템플릿 검색 (예: "Vector3Template")
        var templateKey = $"{propertyType.Name}Template";
        
        // 2. 현재 리소스에서 검색
        if (TryFindResource(templateKey) is DataTemplate template)
            return template;

        // 3. Application 리소스에서 검색
        if (Application.Current.TryFindResource(templateKey) is DataTemplate appTemplate)
            return appTemplate;

        // 4. 기본 타입 매핑 검색
        return FindDefaultTemplateForType(propertyType);
    }

    private DataTemplate? FindDefaultTemplateForType(Type propertyType)
    {
        // 기본 타입별 템플릿 매핑
        var templateKey = propertyType switch
        {
            Type t when t == typeof(DateTime) => "DateTimeTemplate",
            Type t when t == typeof(TimeSpan) => "TimeSpanTemplate",
            Type t when t == typeof(System.Windows.Media.Color) => "ColorTemplate",
            Type t when t.IsEnum => "EnumTemplate",
            _ => null
        };

        if (templateKey != null)
        {
            if (TryFindResource(templateKey) is DataTemplate template)
                return template;
            if (Application.Current.TryFindResource(templateKey) is DataTemplate appTemplate)
                return appTemplate;
        }

        return null;
    }

    private FrameworkElement CreateDefaultControl(INodeProperty property)
    {
        // 기본 TextBox 컨트롤 반환
        return CreateTextBox(property);
    }

    private FrameworkElement CreateTextBox(INodeProperty property)
    {
        var textBox = new TextBox
        {
            Text = property.Value?.ToString()
        };
        
        textBox.TextChanged += (s, e) =>
        {
            property.Value = textBox.Text;
        };
        
        return textBox;
    }

    private FrameworkElement CreateNumberBox(INodeProperty property)
    {
        // 숫자 입력 전용 TextBox 구현
        var numberBox = new TextBox
        {
            Text = property.Value?.ToString()
        };
        
        numberBox.TextChanged += (s, e) =>
        {
            if (double.TryParse(numberBox.Text, out var number))
            {
                property.Value = number;
            }
        };
        
        return numberBox;
    }

    private FrameworkElement CreateCheckBox(INodeProperty property)
    {
        var checkBox = new CheckBox
        {
            IsChecked = (bool?)property.Value
        };
        
        checkBox.Checked += (s, e) => property.Value = true;
        checkBox.Unchecked += (s, e) => property.Value = false;
        
        return checkBox;
    }

    private FrameworkElement CreateColorPicker(INodeProperty property)
    {
        // 색상 선택기 구현
        // 실제 구현에서는 적절한 색상 선택 컨트롤을 사용
        return new Button { Content = "색상 선택" };
    }

    private FrameworkElement CreateComboBox(INodeProperty property)
    {
        // 콤보박스 구현
        return new ComboBox();
    }

    private FrameworkElement CreateMultilineTextBox(INodeProperty property)
    {
        var textBox = new TextBox
        {
            Text = property.Value?.ToString(),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 60
        };
        
        textBox.TextChanged += (s, e) =>
        {
            property.Value = textBox.Text;
        };
        
        return textBox;
    }
}
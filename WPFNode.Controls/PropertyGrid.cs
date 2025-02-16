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

namespace WPFNode.Controls;

public class PropertyGrid : Control, INotifyPropertyChanged
{
    private string _searchText = string.Empty;
    private NodeViewModel? _selectedNode;
    private ListCollectionView? _filteredProperties;

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
            .Select(p => new NodePropertyItem(p))
            .ToList() ?? new List<NodePropertyItem>();

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

        if (item is NodePropertyItem propertyItem)
        {
            return propertyItem.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class NodePropertyItem : INotifyPropertyChanged
{
    private readonly INodeProperty _property;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NodePropertyItem(INodeProperty property)
    {
        _property = property;

        if (_property is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Value))
                {
                    OnPropertyChanged(nameof(Value));
                }
            };
        }
    }

    public string DisplayName => _property.DisplayName;
    public NodePropertyControlType ControlType => _property.ControlType;
    public string? Format => _property.Format;
    public bool CanConnectToPort => _property.CanConnectToPort;

    public object? Value
    {
        get => _property.GetValue();
        set
        {
            if (!Equals(_property.GetValue(), value))
            {
                _property.SetValue(value);
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public DataTemplate? ControlTemplate
    {
        get
        {
            var key = ControlType switch
            {
                NodePropertyControlType.TextBox => "TextBoxTemplate",
                NodePropertyControlType.NumberBox => "NumberBoxTemplate",
                NodePropertyControlType.CheckBox => "CheckBoxTemplate",
                NodePropertyControlType.ColorPicker => "ColorPickerTemplate",
                NodePropertyControlType.ComboBox => "ComboBoxTemplate",
                NodePropertyControlType.MultilineText => "MultilineTextTemplate",
                _ => "TextBoxTemplate"
            };

            return Application.Current.FindResource(key) as DataTemplate;
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
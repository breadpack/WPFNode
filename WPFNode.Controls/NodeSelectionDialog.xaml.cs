using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WPFNode.Core.Interfaces;
using WPFNode.Core.Models;
using WPFNode.Plugin.SDK;

namespace WPFNode.Controls
{
    public partial class NodeSelectionDialog : Window
    {
        private readonly INodePluginService _pluginService;
        private readonly List<NodeCategoryItem> _allNodes;
        private NodeMetadata? _selectedNode;

        public NodeSelectionDialog(INodePluginService pluginService)
        {
            InitializeComponent();
            _pluginService = pluginService;
            _allNodes = CreateNodeTree();
            NodeTreeView.ItemsSource = _allNodes;
            NodeTreeView.SelectedItemChanged += OnNodeTreeViewSelectedItemChanged;
        }

        public Type? SelectedNodeType => _selectedNode?.NodeType;

        private List<NodeCategoryItem> CreateNodeTree()
        {
            var categories = new Dictionary<string, NodeCategoryItem>();

            // 카테고리 생성
            foreach (var category in _pluginService.GetCategories())
            {
                var categoryPath = category.Split('/');
                NodeCategoryItem? currentItem = null;

                foreach (var categoryName in categoryPath)
                {
                    var path = currentItem == null ? categoryName : $"{currentItem.Path}/{categoryName}";
                    
                    if (!categories.TryGetValue(path, out var categoryItem))
                    {
                        categoryItem = new NodeCategoryItem
                        {
                            Name = categoryName,
                            Path = path,
                            IsCategory = true
                        };
                        categories[path] = categoryItem;

                        if (currentItem != null)
                            currentItem.Children.Add(categoryItem);
                    }
                    currentItem = categoryItem;
                }
            }

            // 노드 메타데이터 추가
            foreach (var category in _pluginService.GetCategories())
            {
                var nodeMetadataList = _pluginService.GetNodeMetadataByCategory(category);
                var categoryItem = categories[category];

                foreach (var metadata in nodeMetadataList)
                {
                    categoryItem.Children.Add(new NodeCategoryItem
                    {
                        Name = metadata.Name,
                        Path = $"{category}/{metadata.Name}",
                        IsCategory = false,
                        NodeMetadata = metadata,
                        Description = metadata.Description
                    });
                }
            }

            return categories.Values.Where(c => !c.Path.Contains("/")).ToList();
        }

        private void OnNodeTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is NodeCategoryItem item && !item.IsCategory)
            {
                _selectedNode = item.NodeMetadata;
            }
            else
            {
                _selectedNode = null;
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                NodeTreeView.ItemsSource = _allNodes;
                return;
            }

            var filteredNodes = FilterNodes(_allNodes, searchText);
            NodeTreeView.ItemsSource = filteredNodes;
        }

        private List<NodeCategoryItem> FilterNodes(List<NodeCategoryItem> nodes, string searchText)
        {
            var result = new List<NodeCategoryItem>();

            foreach (var node in nodes)
            {
                if (node.IsCategory)
                {
                    var filteredChildren = FilterNodes(node.Children, searchText);
                    if (filteredChildren.Any() || node.Name.ToLower().Contains(searchText))
                    {
                        var filteredNode = node.Clone();
                        filteredNode.Children = filteredChildren;
                        result.Add(filteredNode);
                    }
                }
                else if (node.Name.ToLower().Contains(searchText) ||
                         (node.Description?.ToLower().Contains(searchText) ?? false))
                {
                    result.Add(node);
                }
            }

            return result;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (_selectedNode != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class NodeCategoryItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsCategory { get; set; }
        public string? Description { get; set; }
        public NodeMetadata? NodeMetadata { get; set; }
        public List<NodeCategoryItem> Children { get; set; } = new();

        public NodeCategoryItem Clone()
        {
            return new NodeCategoryItem
            {
                Name = Name,
                Path = Path,
                IsCategory = IsCategory,
                Description = Description,
                NodeMetadata = NodeMetadata,
                Children = new List<NodeCategoryItem>(Children)
            };
        }
    }
} 
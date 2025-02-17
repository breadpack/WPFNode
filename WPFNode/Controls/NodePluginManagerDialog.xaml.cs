using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using WPFNode.Interfaces;

namespace WPFNode.Controls
{
    public partial class NodePluginManagerDialog : Window
    {
        private readonly INodePluginService _pluginService;
        private readonly ObservableCollection<PluginInfo> _plugins;

        public NodePluginManagerDialog(INodePluginService pluginService)
        {
            InitializeComponent();
            _pluginService = pluginService;
            _plugins = new ObservableCollection<PluginInfo>();
            PluginListView.ItemsSource = _plugins;
            LoadPlugins();
        }

        private void LoadPlugins()
        {
            _plugins.Clear();
            var groupedNodes = _pluginService.GetAllNodeMetadata()
                .GroupBy(m => m.Category)
                .OrderBy(g => g.Key);

            foreach (var group in groupedNodes)
            {
                foreach (var metadata in group.OrderBy(m => m.Name))
                {
                    _plugins.Add(new PluginInfo
                    {
                        Name = metadata.Name,
                        Category = metadata.Category,
                        Description = metadata.Description,
                        NodeType = metadata.NodeType
                    });
                }
            }
        }

        private void OnAddPluginClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "DLL 파일|*.dll",
                Title = "플러그인 DLL 선택"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _pluginService.LoadPlugins(dialog.FileName);
                    LoadPlugins();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"플러그인 로드 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnRemovePluginClick(object sender, RoutedEventArgs e)
        {
            var selectedPlugin = PluginListView.SelectedItem as PluginInfo;
            if (selectedPlugin?.NodeType != null)
            {
                try
                {
                    // 현재는 플러그인 제거 기능이 구현되어 있지 않으므로 메시지만 표시
                    MessageBox.Show("현재 버전에서는 플러그인 제거 기능이 지원되지 않습니다.", 
                        "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"플러그인 제거 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class PluginInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public System.Type? NodeType { get; set; }
    }
} 
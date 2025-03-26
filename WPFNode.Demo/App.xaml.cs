using System.Windows;
using System.Windows.Input;
using WPFNode.Demo.Models;
using WPFNode.Demo.Nodes;
using WPFNode.Services;

namespace WPFNode.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e) {
        Application.Current.DispatcherUnhandledException += (sender, args) => {
            MessageBox.Show(args.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        
        base.OnStartup(e);
        
        // 노드 서비스 초기화
        NodeServices.Initialize("Plugins");
        
        // 커스텀 노드 직접 등록
        RegisterCustomNodes();
        
        // 타입 레지스트리 초기화
        var typeRegistry = TypeRegistry.Instance;
        typeRegistry.InitializeAsync().Wait();
        
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
    
    /// <summary>
    /// 커스텀 노드를 PluginService에 직접 등록합니다.
    /// </summary>
    private void RegisterCustomNodes()
    {
        var pluginService = NodeServices.PluginService;
        
        // 변환 노드
        pluginService.RegisterNodeType(typeof(IntToEmployeeNode));
        pluginService.RegisterNodeType(typeof(StringToEmployeeNode));
        pluginService.RegisterNodeType(typeof(EmployeeInfoNode));
        pluginService.RegisterNodeType(typeof(EmployeeToStringNode));
        pluginService.RegisterNodeType(typeof(EmployeeArrayElementNode));
        
        // 변수 노드
        pluginService.RegisterNodeType(typeof(EmployeeVariableNode));
        pluginService.RegisterNodeType(typeof(IntVariableNode));
        pluginService.RegisterNodeType(typeof(StringVariableNode));
    }
    
    // 노드 타입들을 직접 등록 방식으로 변경하여 EnsureNodeTypesReferenced 메서드는 더 이상 필요 없습니다.
}

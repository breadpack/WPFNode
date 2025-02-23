using System.Collections.ObjectModel;
using WPFNode.Models;
using WPFNode.ViewModels.Nodes;
using IWpfCommand = System.Windows.Input.ICommand;

namespace WPFNode.Interfaces;

public interface INodeCanvasViewModel
{
    ObservableCollection<NodeViewModel> Nodes { get; }
    ObservableCollection<ConnectionViewModel> Connections { get; }
    ObservableCollection<NodeGroupViewModel> Groups { get; }
    
    double Scale { get; set; }
    double OffsetX { get; set; }
    double OffsetY { get; set; }
    NodeViewModel? SelectedNode { get; set; }

    IWpfCommand AddNodeCommand { get; }
    IWpfCommand RemoveNodeCommand { get; }
    IWpfCommand ConnectCommand { get; }
    IWpfCommand DisconnectCommand { get; }
    IWpfCommand AddGroupCommand { get; }
    IWpfCommand RemoveGroupCommand { get; }
    IWpfCommand UndoCommand { get; }
    IWpfCommand RedoCommand { get; }
    IWpfCommand ExecuteCommand { get; }
    IWpfCommand CopyCommand { get; }
    IWpfCommand PasteCommand { get; }
    IWpfCommand SaveCommand { get; }
    IWpfCommand LoadCommand { get; }

    NodePortViewModel FindPortViewModel(IPort port);
    void OnPortsChanged();
    NodeCanvas Model { get; }

    /// <summary>
    /// 지정된 파일 경로에 캔버스를 저장합니다.
    /// </summary>
    /// <param name="filePath">저장할 파일 경로</param>
    /// <returns>저장 작업의 Task</returns>
    Task SaveAsync(string filePath);

    /// <summary>
    /// 지정된 파일 경로에서 캔버스를 불러옵니다.
    /// </summary>
    /// <param name="filePath">불러올 파일 경로</param>
    /// <returns>불러오기 작업의 Task</returns>
    Task LoadAsync(string filePath);

    /// <summary>
    /// 현재 캔버스 상태를 JSON 문자열로 직렬화합니다.
    /// </summary>
    /// <returns>캔버스의 JSON 표현</returns>
    Task<string> ToJsonAsync();

    /// <summary>
    /// JSON 문자열에서 캔버스 상태를 복원합니다.
    /// </summary>
    /// <param name="json">캔버스의 JSON 표현</param>
    Task LoadFromJsonAsync(string json);
} 
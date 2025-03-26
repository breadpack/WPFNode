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
    ObservableCollection<ISelectable> SelectableItems { get; }
    ObservableCollection<ISelectable> SelectedItems { get; }
    
    double Scale { get; set; }
    double OffsetX { get; set; }
    double OffsetY { get; set; }

    IWpfCommand AddNodeCommand { get; }
    IWpfCommand AddNodeAtCommand { get; }
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
    IWpfCommand DuplicateCommand { get; }
    IWpfCommand SaveCommand { get; }
    IWpfCommand LoadCommand { get; }

    NodePortViewModel FindPortViewModel(IPort port);
    void OnPortsChanged();
    NodeCanvas Model { get; }

    /// <summary>
    /// 항목이 선택되었는지 확인합니다.
    /// </summary>
    /// <param name="item">확인할 항목</param>
    /// <returns>항목이 선택되었으면 true, 아니면 false</returns>
    bool IsItemSelected(ISelectable item);

    /// <summary>
    /// 모든 선택 가능한 항목의 선택을 해제합니다.
    /// </summary>
    void ClearSelection();
    
    /// <summary>
    /// 선택된 모든 항목을 반환합니다.
    /// </summary>
    IEnumerable<ISelectable> GetSelectedItems();
    
    /// <summary>
    /// 지정된 유형의 선택된 항목을 반환합니다.
    /// </summary>
    IEnumerable<T> GetSelectedItemsOfType<T>() where T : ISelectable;
    
    /// <summary>
    /// 항목을 선택합니다.
    /// </summary>
    /// <param name="selectable">선택할 항목</param>
    /// <param name="clearOthers">다른 항목의 선택을 해제할지 여부</param>
    void SelectItem(ISelectable selectable, bool clearOthers = true);
    
    /// <summary>
    /// 항목의 선택을 해제합니다.
    /// </summary>
    /// <param name="selectable">선택 해제할 항목</param>
    void DeselectItem(ISelectable selectable);
    
    /// <summary>
    /// ID로 항목을 찾아 반환합니다.
    /// </summary>
    ISelectable? FindSelectableById(Guid id);

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

    /// <summary>
    /// ID로 노드를 찾습니다.
    /// </summary>
    /// <param name="nodeId">찾을 노드의 ID</param>
    /// <returns>찾은 노드의 ViewModel, 없으면 null</returns>
    NodeViewModel? FindNodeById(Guid nodeId);

    /// <summary>
    /// 이름으로 노드를 찾습니다.
    /// </summary>
    /// <param name="name">찾을 노드의 이름</param>
    /// <returns>찾은 노드들의 ViewModel 목록</returns>
    IEnumerable<NodeViewModel> FindNodesByName(string name);

    /// <summary>
    /// 타입으로 노드를 찾습니다.
    /// </summary>
    /// <param name="nodeType">찾을 노드의 타입</param>
    /// <returns>찾은 노드들의 ViewModel 목록</returns>
    IEnumerable<NodeViewModel> FindNodesByType(Type nodeType);
}

using System.Collections.ObjectModel;
using WPFNode.Models;
using WPFNode.ViewModels.Nodes;

namespace WPFNode.Interfaces;

public interface INodeViewModel
{
    /// <summary>
    /// 노드의 ID를 가져옵니다.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// 노드의 이름을 가져오거나 설정합니다.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// 노드의 입력 포트 목록을 가져옵니다.
    /// </summary>
    ReadOnlyObservableCollection<NodePortViewModel> InputPorts { get; }

    /// <summary>
    /// 노드의 출력 포트 목록을 가져옵니다.
    /// </summary>
    ReadOnlyObservableCollection<NodePortViewModel> OutputPorts { get; }

    /// <summary>
    /// 노드의 모델을 가져옵니다.
    /// </summary>
    INode Model { get; }

    /// <summary>
    /// 노드의 포트 정보를 조회합니다.
    /// </summary>
    /// <returns>포트 정보 (입력 포트와 출력 포트 목록)</returns>
    (ReadOnlyObservableCollection<NodePortViewModel> InputPorts, ReadOnlyObservableCollection<NodePortViewModel> OutputPorts) GetPorts();

    /// <summary>
    /// 노드의 타입 정보를 조회합니다.
    /// </summary>
    /// <returns>노드의 타입 정보</returns>
    Type GetNodeType();
} 
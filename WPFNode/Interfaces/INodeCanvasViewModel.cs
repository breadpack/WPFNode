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
} 
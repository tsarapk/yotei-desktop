using Avalonia.Controls;
using Avalonia.Media;
using YoteiTasks.Models;
using YoteiTasks.ValueObjects;
using YoteiTasks.ViewModels;

namespace YoteiTasks.Services;


public class SelectionManager
{
    private readonly NodeRenderer _nodeRenderer;
    private GraphNode? _selectedNode;
    private MainViewModel? _mainViewModel;

    public GraphNode? SelectedNode => _selectedNode;

    public SelectionManager(NodeRenderer nodeRenderer)
    {
        _nodeRenderer = nodeRenderer;
    }

    public void SetMainViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public void SelectNode(GraphNode node)
    {
        
        if (_selectedNode != null)
        {
            DeselectNode(_selectedNode);
        }

        _selectedNode = node;
        
        
        var shape = _nodeRenderer.GetNodeShape(node.Id);
        if (shape != null)
        {
            shape.Background = LocalColors.NodeBackSelected;
        }

        
        node.OnSelect();

        
        if (_mainViewModel != null)
        {
            _mainViewModel.SelectedNode = node;
        }
    }

    public void DeselectNode(GraphNode? node = null)
    {
        var nodeToDeselect = node ?? _selectedNode;
        if (nodeToDeselect == null)
            return;

        
        var shape = _nodeRenderer.GetNodeShape(nodeToDeselect.Id);
        if (shape != null)
        {
            shape.Background = LocalColors.NodeBackDefault;
        }

        
        nodeToDeselect.OnDeselect();

        if (nodeToDeselect == _selectedNode)
        {
            _selectedNode = null;
        }

        
        if (_mainViewModel != null)
        {
            _mainViewModel.SelectedNode = null;
        }
    }

    public void ClearSelection()
    {
        if (_selectedNode != null)
        {
            DeselectNode(_selectedNode);
        }
    }
}










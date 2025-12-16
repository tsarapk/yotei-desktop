using Avalonia;
using YoteiTasks.Adapters;
using YoteiTasks.Models;
using YoteiTasks.ViewModels;

namespace YoteiTasks.Services;


public class NodeFactory
{
    private readonly MainViewModel? _mainViewModel;

    public NodeFactory(MainViewModel? mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public GraphNode? CreateNodeAtPosition(Avalonia.Point position, Graph? graph)
    {
        if (graph == null || _mainViewModel == null)
            return null;

        _mainViewModel.SuppressUpdates = true;
      
        var nodeLabel = $"Задача {graph.Nodes.Count + 1}";
        var newNode = _mainViewModel.CreateTaskNode(nodeLabel, position.X, position.Y);
        
        _mainViewModel.SelectedNode = newNode;
        
        graph.Nodes.Add(newNode);
        YoteiAdapter.SaveNodePosition(newNode.Id, position.X, position.Y);

        _mainViewModel.SuppressUpdates = false;
        return newNode;
    }
}










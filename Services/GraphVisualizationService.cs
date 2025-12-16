using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using YoteiTasks.Models;

namespace YoteiTasks.Services;


public class GraphVisualizationService
{
    private readonly NodeRenderer _nodeRenderer;
    private readonly EdgeRenderer _edgeRenderer;
    private readonly NodeInteractionHandler _interactionHandler;
    private Graph? _currentGraph;

    public GraphVisualizationService(
        NodeRenderer nodeRenderer,
        EdgeRenderer edgeRenderer,
        NodeInteractionHandler interactionHandler)
    {
        _nodeRenderer = nodeRenderer;
        _edgeRenderer = edgeRenderer;
        _interactionHandler = interactionHandler;
    }

    public void SetGraph(Graph? graph)
    {
        if (_currentGraph != null)
        {
            UnsubscribeFromGraph(_currentGraph);
        }

        _currentGraph = graph;

        if (_currentGraph != null)
        {
            SubscribeToGraph(_currentGraph);
        }

        RefreshGraph();
    }

    public void RefreshGraph()
    {
        _nodeRenderer.Clear();
        _edgeRenderer.Clear();

        if (_currentGraph == null)
            return;

        
        foreach (var edge in _currentGraph.Edges)
        {
            var sourceNode = _currentGraph.Nodes.FirstOrDefault(n => n.Id == edge.SourceId);
            var targetNode = _currentGraph.Nodes.FirstOrDefault(n => n.Id == edge.TargetId);

            if (sourceNode != null && targetNode != null)
            {
                _edgeRenderer.AddEdge(edge, sourceNode, targetNode);
            }
        }

       
        foreach (var node in _currentGraph.Nodes)
        {
            AddNode(node);
        }
    }

    public void AddNode(GraphNode node)
    {
        _nodeRenderer.AddNode(node);
        
       
        var shape = _nodeRenderer.GetNodeShape(node.Id);
        if (shape != null)
        {
            _interactionHandler.AttachToNode(node, shape);
        }

   
        node.PropertyChanged += Node_PropertyChanged;
    }

    public void RemoveNode(GraphNode node)
    {
        node.PropertyChanged -= Node_PropertyChanged;
        _nodeRenderer.RemoveNode(node.Id);
        _edgeRenderer.RemoveEdgesForNode(node.Id);
    }

    private void SubscribeToGraph(Graph graph)
    {
        graph.Nodes.CollectionChanged += Nodes_CollectionChanged;
        graph.Edges.CollectionChanged += Edges_CollectionChanged;

        foreach (var node in graph.Nodes)
        {
            node.PropertyChanged += Node_PropertyChanged;
        }
    }

    private void UnsubscribeFromGraph(Graph graph)
    {
        graph.Nodes.CollectionChanged -= Nodes_CollectionChanged;
        graph.Edges.CollectionChanged -= Edges_CollectionChanged;

        foreach (var node in graph.Nodes)
        {
            node.PropertyChanged -= Node_PropertyChanged;
        }
    }

    private void Nodes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (GraphNode node in e.NewItems)
            {
                AddNode(node);
            }
        }

        if (e.OldItems != null)
        {
            foreach (GraphNode node in e.OldItems)
            {
                RemoveNode(node);
            }
        }
    }

    private void Edges_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RefreshGraph();
    }

    private void Node_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is GraphNode node)
        {
            if (e.PropertyName == nameof(GraphNode.X) || e.PropertyName == nameof(GraphNode.Y))
            {
                _nodeRenderer.UpdateNodePosition(node);
                
                if (_currentGraph != null)
                {
                    _edgeRenderer.UpdateConnectedEdges(node, _currentGraph);
                }
            }

            _nodeRenderer.RefreshNodeVisualization(node);
        }
    }
}










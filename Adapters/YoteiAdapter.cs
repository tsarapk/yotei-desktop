using System.Collections.Generic;
using System.Linq;
using YoteiLib.Core;
using YoteiTasks.Models;

namespace YoteiTasks.Adapters;




public static class YoteiAdapter
{
    
    private static readonly Dictionary<string, Point> _nodePositions = new();

    
    public static GraphNode FromTaskNode(TaskNode taskNode, Point? position = null)
    {
        var nodeId = taskNode.Id.ToString();
        var positionToUse = position ?? _nodePositions.GetValueOrDefault(nodeId) ?? new Point { X = 0, Y = 0 };
        
        var graphNode = new GraphNode(nodeId, taskNode.Title, positionToUse.X, positionToUse.Y)
        {
            TaskNode = taskNode
        };
        
        
        _nodePositions[nodeId] = positionToUse;
        
        return graphNode;
    }

    
    public static void SaveNodePosition(string nodeId, double x, double y)
    {
        _nodePositions[nodeId] = new YoteiLib.Core.Point { X = x, Y = y };
    }

    
    public static void UpdateTaskNode(GraphNode graphNode)
    {
        if (graphNode.TaskNode != null)
        {
            graphNode.TaskNode.SetTitle(graphNode.Label);
            SaveNodePosition(graphNode.Id, graphNode.X, graphNode.Y);
        }
    }


    public static GraphEdge FromTaskEdge(TaskEdge taskEdge)
    {
        return new GraphEdge(
            taskEdge.From.ToString(),
            taskEdge.To.ToString()
        );
    }


    public static Graph FromTaskRepository(TaskRepository repository, string graphName = "Tasks Graph")
    {
        var graph = new Graph(graphName);
        

        var allTasks = repository.GetAll();
        foreach (var task in allTasks)
        {
            var nodeId = task.Id.ToString();
            var position = _nodePositions.GetValueOrDefault(nodeId) ?? new YoteiLib.Core.Point { X = 0, Y = 0 };
            var node = FromTaskNode(task, position);
            graph.Nodes.Add(node);
        }
        
  
        foreach (var task in allTasks)
        {
            var taskId = task.Id.ToString();
            
      
            foreach (var outgoing in repository.GetOutgoing(task))
            {
                var edge = new GraphEdge(taskId, outgoing.Id.ToString());
                if (!graph.Edges.Any(e => e.SourceId == edge.SourceId && e.TargetId == edge.TargetId))
                {
                    graph.Edges.Add(edge);
                }
            }
        }
        
        return graph;
    }

 
    public static void SyncGraphToRepository(Graph graph, TaskRepository repository)
    {
        
        foreach (var node in graph.Nodes)
        {
            if (node.TaskNode != null)
            {
                UpdateTaskNode(node);
            }
        }
    }


    public static GraphNode CreateTaskNode(TaskRepository repository, string title, double x, double y)
    {
        var task = repository.Create(title);
        var node = FromTaskNode(task, new YoteiLib.Core.Point { X = x, Y = y });
        SaveNodePosition(node.Id, x, y);
        return node;
    }


    public static Result DeleteTaskNode(GraphNode node, TaskRepository repository)
    {
        if (node.TaskNode != null)
        {
            var result = repository.Delete(node.TaskNode);
            if (result)
            {
                _nodePositions.Remove(node.Id);
            }
            return result;
        }
        return Result.TaskNotFound;
    }


    public static void AddRelation(GraphNode fromNode, GraphNode toNode, TaskRepository repository, EdgeType edgeType = EdgeType.Block)
    {
        if (fromNode.TaskNode != null && toNode.TaskNode != null)
        {
            repository.AddRelation(fromNode.TaskNode, toNode.TaskNode, edgeType);
        }
    }

    
    public static EdgeType? GetEdgeType(GraphNode fromNode, GraphNode toNode, TaskRepository repository)
    {
        if (fromNode.TaskNode == null || toNode.TaskNode == null)
            return null;

        var outgoing = repository.GetOutgoing(fromNode.TaskNode);
        var edge = outgoing.FirstOrDefault(t => t.Id == toNode.TaskNode.Id);
        
        
     
        return null;
    }
}


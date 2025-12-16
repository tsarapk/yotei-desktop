using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace YoteiTasks.Models;


public class Graph : INotifyPropertyChanged
{
    private string _name;
    private ObservableCollection<GraphNode> _nodes;
    private ObservableCollection<GraphEdge> _edges;

    public ObservableCollection<GraphNode> AvaibleNodes(GraphNode node)
    {
        var relatedIds = new HashSet<string>(
            Edges
                .Where(e => e.SourceId == node.Id)   
                .Select(e => e.TargetId)
                .Concat(
                    Edges
                        .Where(e => e.TargetId == node.Id) 
                        .Select(e => e.SourceId)
                )
        );
        
        var available = Nodes
            .Where(n => n.Id != node.Id && !relatedIds.Contains(n.Id))
            .ToList();

        return new ObservableCollection<GraphNode>(available);
    }

    
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public ObservableCollection<GraphNode> Nodes
    {
        get => _nodes;
        set => SetProperty(ref _nodes, value);
    }

    public ObservableCollection<GraphEdge> Edges
    {
        get => _edges;
        set => SetProperty(ref _edges, value);
    }

    public Graph(string name)
    {
        _name = name;
        _nodes = new ObservableCollection<GraphNode>();
        _edges = new ObservableCollection<GraphEdge>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}


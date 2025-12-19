using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YoteiTasks.Models;




public class ProjectActorRole
{
    public string ActorId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
}




public class Project : INotifyPropertyChanged
{
    private string _id;
    private string _name;
    private Graph _graph;
    private List<ProjectActorRole> _actorRoles;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public Graph Graph
    {
        get => _graph;
        set => SetProperty(ref _graph, value);
    }

    public List<ProjectActorRole> ActorRoles
    {
        get => _actorRoles;
        set => SetProperty(ref _actorRoles, value);
    }

    public Project(string name, Graph graph)
    {
        _id = Guid.NewGuid().ToString();
        _name = name;
        _graph = graph;
        _actorRoles = new List<ProjectActorRole>();
    }

    public Project(string id, string name, Graph graph, List<ProjectActorRole> actorRoles)
    {
        _id = id;
        _name = name;
        _graph = graph;
        _actorRoles = actorRoles;
    }

    public void AddActorRole(string actorId, string roleId)
    {
        
        _actorRoles.RemoveAll(ar => ar.ActorId == actorId);
        
        _actorRoles.Add(new ProjectActorRole
        {
            ActorId = actorId,
            RoleId = roleId
        });
        
        OnPropertyChanged(nameof(ActorRoles));
    }

    public void RemoveActor(string actorId)
    {
        _actorRoles.RemoveAll(ar => ar.ActorId == actorId);
        OnPropertyChanged(nameof(ActorRoles));
    }

    public string? GetActorRole(string actorId)
    {
        return _actorRoles.Find(ar => ar.ActorId == actorId)?.RoleId;
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

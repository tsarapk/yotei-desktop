using System;
using System.Collections.Generic;
using YoteiLib.Core;
using YoteiTasks.Models;

namespace YoteiTasks.Services;




public class SaveData
{
    
    
    
    public List<GraphSaveData> Graphs { get; set; } = new();

    
    
    
    public List<ProjectSaveData> Projects { get; set; } = new();

    
    
    
    public List<ResourceSaveData> Resources { get; set; } = new();

    
    
    
    public List<ActorSaveData> Actors { get; set; } = new();

    
    
    
    public List<RoleSaveData> Roles { get; set; } = new();

    
    
    
    public string? SelectedGraphId { get; set; }
    
    
    
    
    public string? SelectedProjectId { get; set; }
}




public class ResourceSaveData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
}




public class ActorSaveData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsSuperUser { get; set; }
}




public class RoleSaveData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Strength { get; set; }
    public List<RolePriv> Privileges { get; set; } = new();
}




public class GraphSaveData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public List<NodeSaveData> Nodes { get; set; } = new();
    public List<EdgeSaveData> Edges { get; set; } = new();
}


public class NodeSaveData
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    
    
    public string? TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset? Deadline { get; set; }
    public string? ActorId { get; set; }
    public bool IsCompleted { get; set; }
}


public class EdgeSaveData
{
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string EdgeType { get; set; } = "Block";
}




public class ProjectSaveData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GraphId { get; set; } = string.Empty;
    public List<ProjectActorRoleSaveData> ActorRoles { get; set; } = new();
}




public class ProjectActorRoleSaveData
{
    public string ActorId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
}









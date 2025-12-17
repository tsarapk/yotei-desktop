using System;
using System.Collections.Generic;
using System.Linq;
using YoteiLib.Core;
using YoteiTasks.Adapters;
using YoteiTasks.Models;

namespace YoteiTasks.Services;




public static class SaveDataConverter
{
    
    private static readonly Dictionary<Graph, string> _graphIds = new();

    
    
    
    public static SaveData ToSaveData(IEnumerable<Graph> graphs, Graph? selectedGraph, TaskRepository repository,
        IEnumerable<Resource>? resources = null, IEnumerable<Actor>? actors = null, IEnumerable<YoteiLib.Core.Role>? roles = null,
        IEnumerable<Project>? projects = null, Project? selectedProject = null, ISuperUserService? superUserService = null)
    {
        var saveData = new SaveData();
        
        
        foreach (var graph in graphs)
        {
            if (graph == null)
                continue;

            if (!_graphIds.TryGetValue(graph, out var graphId))
            {
                graphId = Guid.NewGuid().ToString();
                _graphIds[graph] = graphId;
            }
            
            var graphSaveData = ToGraphSaveData(graph, repository);
            graphSaveData.Id = graphId;
            saveData.Graphs.Add(graphSaveData);
            
            if (graph == selectedGraph)
            {
                saveData.SelectedGraphId = graphId;
            }
        }
        
        
        if (resources != null)
        {
            foreach (var resource in resources)
            {
                if (resource == null)
                    continue;

                saveData.Resources.Add(new ResourceSaveData
                {
                    Id = resource.Id.ToString(),
                    Name = resource.Name,
                    Value = resource.Value
                });
            }
        }

        
        if (roles != null)
        {
            foreach (var role in roles)
            {
                if (role == null)
                    continue;
                try
                {
                    var privs = role.privs ?? new List<RolePriv>();

                    saveData.Roles.Add(new RoleSaveData
                    {
                        Id = role.Id.ToString(),
                        Name = role.Name,
                        Strength = role.Strength,
                        Privileges = new List<RolePriv>(privs)
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при сохранении роли '{role?.Name}': {ex}");
                }
            }
        }
        
        
        if (actors != null)
        {
            foreach (var actor in actors)
            {
                if (actor == null)
                    continue;

                saveData.Actors.Add(new ActorSaveData
                {
                    Id = actor.Id.ToString(),
                    Name = actor.Name,
                    Username = actor.Username,
                    PasswordHash = actor.PasswordHash,
                    IsSuperUser = superUserService?.IsSuperUser(actor) ?? false
                });
            }
        }
        
        
        if (projects != null)
        {
            foreach (var project in projects)
            {
                if (project == null)
                    continue;

                var graphId = GetGraphId(project.Graph);
                
                var projectSaveData = new ProjectSaveData
                {
                    Id = project.Id,
                    Name = project.Name,
                    GraphId = graphId,
                    ActorRoles = project.ActorRoles.Select(ar => new ProjectActorRoleSaveData
                    {
                        ActorId = ar.ActorId,
                        RoleId = ar.RoleId
                    }).ToList()
                };
                
                saveData.Projects.Add(projectSaveData);
            }
            
            if (selectedProject != null)
            {
                saveData.SelectedProjectId = selectedProject.Id;
            }
        }
        
        return saveData;
    }

    
    
    
    public static GraphSaveData ToGraphSaveData(Graph graph, TaskRepository repository)
    {
        var graphSaveData = new GraphSaveData
        {
            Name = graph.Name
        };

        
        foreach (var node in graph.Nodes)
        {
            var nodeSaveData = ToNodeSaveData(node);
            graphSaveData.Nodes.Add(nodeSaveData);
        }

        
        foreach (var edge in graph.Edges)
        {
            var edgeSaveData = ToEdgeSaveData(edge, repository);
            graphSaveData.Edges.Add(edgeSaveData);
        }

        return graphSaveData;
    }

    
    
    
    public static NodeSaveData ToNodeSaveData(GraphNode node)
    {
        var nodeSaveData = new NodeSaveData
        {
            Id = node.Id,
            Label = node.Label,
            X = node.X,
            Y = node.Y
        };

        
        if (node.TaskNode != null)
        {
            nodeSaveData.TaskId = node.TaskNode.Id.ToString();
            nodeSaveData.Title = node.TaskNode.Title;
            nodeSaveData.Status = node.TaskNode.Status.ToString();
            nodeSaveData.Priority = node.TaskNode.Priority;
            nodeSaveData.Payload = node.TaskNode.Payload ?? string.Empty;
            nodeSaveData.Deadline = node.TaskNode.Deadline != DateTimeOffset.MaxValue 
                ? node.TaskNode.Deadline 
                : null;
            nodeSaveData.IsCompleted = node.TaskNode.IsCompleted;
            
            if (node.TaskNode.Meta?.PerfomedBy != null)
            {
                nodeSaveData.ActorId = node.TaskNode.Meta.PerfomedBy.Id.ToString();
            }
        }
        
        // Save resource usages
        foreach (var resourceUsage in node.ResourceUsages)
        {
            nodeSaveData.ResourceUsages.Add(new TaskResourceUsageSaveData
            {
                ResourceId = resourceUsage.Resource.Id.ToString(),
                Amount = resourceUsage.Amount
            });
            Console.WriteLine($"[SaveDataConverter] Сохранение ресурса '{resourceUsage.Resource.Name}' (количество: {resourceUsage.Amount}) для узла '{node.Label}'");
        }

        return nodeSaveData;
    }

    
    
    
    public static EdgeSaveData ToEdgeSaveData(GraphEdge edge, TaskRepository repository)
    {
        var edgeSaveData = new EdgeSaveData
        {
            SourceId = edge.SourceId,
            TargetId = edge.TargetId,
            EdgeType = "Block" 
        };

        
        try
        {
            if (Guid.TryParse(edge.SourceId, out var sourceGuid) && 
                Guid.TryParse(edge.TargetId, out var targetGuid))
            {
                
                var allTasks = repository.GetAll();
                var sourceTask = allTasks.FirstOrDefault(t => t.Id.ToString() == edge.SourceId);
                var targetTask = allTasks.FirstOrDefault(t => t.Id.ToString() == edge.TargetId);
                
                if (sourceTask != null && targetTask != null)
                {
                    
                    var outgoing = repository.GetOutgoing(sourceTask);
                    var relatedTask = outgoing.FirstOrDefault(t => t.Id.ToString() == edge.TargetId);
                    
                    if (relatedTask != null)
                    {
                        
                        edgeSaveData.EdgeType = "Block";
                    }
                }
            }
        }
        catch
        {
            
        }

        return edgeSaveData;
    }

    
    
    
    public static (List<Graph> graphs, List<Resource> resources, List<Actor> actors, List<Project> projects)
        FromSaveData(SaveData saveData, TaskRepository repository, Yotei yotei, ISuperUserService? superUserService = null)
    {
        var graphs = new List<Graph>();
        var graphIdMap = new Dictionary<string, Graph>();
        var resources = new List<Resource>();
        var actors = new List<Actor>();
        var projects = new List<Project>();

        
        // Create a mapping from saved resource IDs to actual resources
        var resourceIdMap = new Dictionary<string, Resource>();
        
        if (saveData.Resources != null)
        {
            Console.WriteLine($"[SaveDataConverter] Загрузка {saveData.Resources.Count} ресурсов...");
            foreach (var resourceData in saveData.Resources)
            {
                // Try to find existing resource by ID first
                var existingResource = yotei.Resources.GetAll().FirstOrDefault(r => r.Id.ToString() == resourceData.Id);
                
                if (existingResource != null)
                {
                    // Update existing resource
                    existingResource.SetName(resourceData.Name);
                    existingResource.SetValue(resourceData.Value);
                    resources.Add(existingResource);
                    resourceIdMap[resourceData.Id] = existingResource;
                    Console.WriteLine($"[SaveDataConverter] Обновлен существующий ресурс '{resourceData.Name}' (ID: {resourceData.Id})");
                }
                else
                {
                    // Create new resource - ID will be different
                    var resource = yotei.Resources.Create();
                    resource.SetName(resourceData.Name);
                    resource.SetValue(resourceData.Value);
                    
                    // Map the saved ID to the new resource
                    resources.Add(resource);
                    resourceIdMap[resourceData.Id] = resource;
                    Console.WriteLine($"[SaveDataConverter] Создан новый ресурс '{resourceData.Name}' (Сохраненный ID: {resourceData.Id}, Новый ID: {resource.Id})");
                }
            }
        }
        
        
        foreach (var graphSaveData in saveData.Graphs)
        {
            var graph = FromGraphSaveData(graphSaveData, repository, resources, resourceIdMap);
            graphs.Add(graph);
            graphIdMap[graphSaveData.Id] = graph;
        }

        
        if (saveData.Actors != null)
        {
            foreach (var actorData in saveData.Actors)
            {
                var actor = yotei.Actors.Create();
                actor.SetName(actorData.Name);
                
                
                if (!string.IsNullOrEmpty(actorData.Username) && !string.IsNullOrEmpty(actorData.PasswordHash))
                {
                    actor.SetCredentials(actorData.Username, actorData.PasswordHash);
                }
                
                
                if (actorData.IsSuperUser && superUserService != null)
                {
                    superUserService.SetSuperUser(actor);
                }
                
                actors.Add(actor);
            }
        }

        
        if (saveData.Roles != null)
        {
            foreach (var roleData in saveData.Roles)
            {
                var role = yotei.Roles.Create();
                role.Name = roleData.Name;
                role.Strength = roleData.Strength;

                role.privs.Clear();
                if (roleData.Privileges != null)
                {
                    foreach (var priv in roleData.Privileges)
                    {
                        if (!role.privs.Contains(priv))
                            role.privs.Add(priv);
                    }
                }
            }
        }

        
        if (saveData.Projects != null)
        {
            foreach (var projectData in saveData.Projects)
            {
                if (graphIdMap.TryGetValue(projectData.GraphId, out var graph))
                {
                    var actorRoles = projectData.ActorRoles.Select(ar => new ProjectActorRole
                    {
                        ActorId = ar.ActorId,
                        RoleId = ar.RoleId
                    }).ToList();
                    
                    var project = new Project(projectData.Id, projectData.Name, graph, actorRoles);
                    projects.Add(project);
                }
            }
        }

        return (graphs, resources, actors, projects);
    }
    
    
    
    
    public static string GetGraphId(Graph graph)
    {
        if (!_graphIds.TryGetValue(graph, out var graphId))
        {
            graphId = Guid.NewGuid().ToString();
            _graphIds[graph] = graphId;
        }
        return graphId;
    }

    
    
    
    public static Graph FromGraphSaveData(GraphSaveData graphSaveData, TaskRepository repository, List<Resource>? resources = null, Dictionary<string, Resource>? resourceIdMap = null)
    {
        var graph = new Graph(graphSaveData.Name);
        
        
        _graphIds[graph] = graphSaveData.Id;

        
        var nodeMap = new Dictionary<string, GraphNode>();

        
        foreach (var nodeSaveData in graphSaveData.Nodes)
        {
            var node = FromNodeSaveData(nodeSaveData, repository, resources, resourceIdMap);
            graph.Nodes.Add(node);
            
            
            
            nodeMap[nodeSaveData.Id] = node;
            nodeMap[node.Id] = node;
            
            
            if (!string.IsNullOrEmpty(nodeSaveData.TaskId))
            {
                nodeMap[nodeSaveData.TaskId] = node;
            }
            
            
            YoteiAdapter.SaveNodePosition(node.Id, nodeSaveData.X, nodeSaveData.Y);
        }

        
        foreach (var edgeSaveData in graphSaveData.Edges)
        {
            
            
            GraphNode? sourceNode = null;
            GraphNode? targetNode = null;
            
            if (nodeMap.TryGetValue(edgeSaveData.SourceId, out sourceNode) &&
                nodeMap.TryGetValue(edgeSaveData.TargetId, out targetNode))
            {
                
                var sourceId = sourceNode.Id;
                var targetId = targetNode.Id;
                
                
                var edgeExists = graph.Edges.Any(e => 
                    e.SourceId == sourceId && e.TargetId == targetId);
                
                if (!edgeExists)
                {
                    var edge = new GraphEdge(sourceId, targetId);
                    graph.Edges.Add(edge);

                    
                    if (sourceNode.TaskNode != null && targetNode.TaskNode != null)
                    {
                        var edgeType = Enum.TryParse<EdgeType>(edgeSaveData.EdgeType, out var parsedType)
                            ? parsedType
                            : EdgeType.Block;
                        
                        
                        var outgoing = repository.GetOutgoing(sourceNode.TaskNode);
                        var relationExists = outgoing.Any(t => t.Id.ToString() == targetNode.TaskNode.Id.ToString());
                        
                        if (!relationExists)
                        {
                            repository.AddRelation(sourceNode.TaskNode, targetNode.TaskNode, edgeType);
                        }
                    }
                }
            }
        }

        return graph;
    }

    
    
    
    public static GraphNode FromNodeSaveData(NodeSaveData nodeSaveData, TaskRepository repository, List<Resource>? resources = null, Dictionary<string, Resource>? resourceIdMap = null)
    {
        GraphNode node;
        TaskNode? taskNode = null;

        
        if (!string.IsNullOrEmpty(nodeSaveData.TaskId))
        {
            var allTasks = repository.GetAll();
            taskNode = allTasks.FirstOrDefault(t => t.Id.ToString() == nodeSaveData.TaskId);
        }

        
        if (taskNode == null && !string.IsNullOrEmpty(nodeSaveData.Title))
        {
            taskNode = repository.Create(nodeSaveData.Title);
        }

        
        if (taskNode != null)
        {
            node = YoteiAdapter.FromTaskNode(taskNode, new Point { X = nodeSaveData.X, Y = nodeSaveData.Y });
            
            
            if (!string.IsNullOrEmpty(nodeSaveData.Title))
            {
                taskNode.SetTitle(nodeSaveData.Title);
            }
            
            if (Enum.TryParse<TaskStatus>(nodeSaveData.Status, out var status))
            {
                taskNode.SetStatusSecure(status);
            }
            
            taskNode.SetPriority(nodeSaveData.Priority);
            taskNode.SetPayload(nodeSaveData.Payload ?? string.Empty);
            
            if (nodeSaveData.Deadline.HasValue)
            {
                
            }
            
            if (nodeSaveData.IsCompleted && !taskNode.IsCompleted)
            {
                repository.TryComplete(taskNode, out _);
            }
            
            
            if (!string.IsNullOrEmpty(nodeSaveData.ActorId) && Guid.TryParse(nodeSaveData.ActorId, out var actorId))
            {
                
                
            }
        }
        else
        {
            
            node = new GraphNode(nodeSaveData.Id, nodeSaveData.Label, nodeSaveData.X, nodeSaveData.Y);
        }
        
        // Load resource usages
        if (nodeSaveData.ResourceUsages != null && nodeSaveData.ResourceUsages.Count > 0)
        {
            Console.WriteLine($"[SaveDataConverter] Загрузка {nodeSaveData.ResourceUsages.Count} использований ресурсов для узла '{nodeSaveData.Label}'");
            foreach (var resourceUsageData in nodeSaveData.ResourceUsages)
            {
                Resource? resource = null;
                
                // First try to find resource using the ID map (handles ID changes)
                if (resourceIdMap != null && resourceIdMap.TryGetValue(resourceUsageData.ResourceId, out var mappedResource))
                {
                    resource = mappedResource;
                    Console.WriteLine($"[SaveDataConverter] Найден ресурс через маппинг: '{resource.Name}' (Сохраненный ID: {resourceUsageData.ResourceId}, Текущий ID: {resource.Id})");
                }
                // Fallback: try to find by current ID
                else if (resources != null)
                {
                    resource = resources.FirstOrDefault(r => r.Id.ToString() == resourceUsageData.ResourceId);
                    if (resource != null)
                    {
                        Console.WriteLine($"[SaveDataConverter] Найден ресурс по текущему ID: '{resource.Name}' (ID: {resource.Id})");
                    }
                }
                
                if (resource != null)
                {
                    node.ResourceUsages.Add(new TaskResourceUsage(resource, resourceUsageData.Amount));
                    Console.WriteLine($"[SaveDataConverter] ✓ Загружен ресурс '{resource.Name}' (количество: {resourceUsageData.Amount})");
                }
                else
                {
                    Console.WriteLine($"[SaveDataConverter] ✗ ОШИБКА: Ресурс с ID '{resourceUsageData.ResourceId}' не найден ни в маппинге, ни в списке ресурсов!");
                }
            }
        }
        else
        {
            Console.WriteLine($"[SaveDataConverter] Нет использований ресурсов для узла '{nodeSaveData.Label}'");
        }

        return node;
    }
}


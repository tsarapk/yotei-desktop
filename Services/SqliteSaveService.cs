using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using YoteiLib.Core;
using YoteiTasks.Models;

namespace YoteiTasks.Services;




public class SqliteSaveService : ISaveService
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    public SqliteSaveService(string? databasePath = null)
    {
        _databasePath = databasePath ?? GetDefaultDatabasePath();
        _connectionString = $"Data Source={_databasePath}";
        
        Console.WriteLine($"[SqliteSaveService] Database path: {_databasePath}");
        InitializeDatabase();
    }

    private static string GetDefaultDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "YoteiTasks");
        Directory.CreateDirectory(appFolder);
        return Path.Combine(appFolder, "yotei.db");
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createTablesCommand = connection.CreateCommand();
        createTablesCommand.CommandText = @"
            -- Graphs table
            CREATE TABLE IF NOT EXISTS Graphs (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL
            );

            -- Nodes table
            CREATE TABLE IF NOT EXISTS Nodes (
                Id TEXT PRIMARY KEY,
                GraphId TEXT NOT NULL,
                Label TEXT NOT NULL,
                X REAL NOT NULL,
                Y REAL NOT NULL,
                TaskId TEXT,
                Title TEXT,
                Status TEXT,
                Priority INTEGER,
                Payload TEXT,
                Deadline TEXT,
                ActorId TEXT,
                IsCompleted INTEGER,
                FOREIGN KEY (GraphId) REFERENCES Graphs(Id) ON DELETE CASCADE
            );

            -- Edges table
            CREATE TABLE IF NOT EXISTS Edges (
                GraphId TEXT NOT NULL,
                SourceId TEXT NOT NULL,
                TargetId TEXT NOT NULL,
                EdgeType TEXT NOT NULL DEFAULT 'Block',
                PRIMARY KEY (GraphId, SourceId, TargetId),
                FOREIGN KEY (GraphId) REFERENCES Graphs(Id) ON DELETE CASCADE
            );

            -- Projects table
            CREATE TABLE IF NOT EXISTS Projects (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                GraphId TEXT,
                FOREIGN KEY (GraphId) REFERENCES Graphs(Id)
            );

            -- ProjectActorRoles table
            CREATE TABLE IF NOT EXISTS ProjectActorRoles (
                ProjectId TEXT NOT NULL,
                ActorId TEXT NOT NULL,
                RoleId TEXT NOT NULL,
                PRIMARY KEY (ProjectId, ActorId),
                FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE,
                FOREIGN KEY (ActorId) REFERENCES Actors(Id) ON DELETE CASCADE,
                FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
            );

            -- Resources table
            CREATE TABLE IF NOT EXISTS Resources (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Value REAL NOT NULL
            );

            -- Actors table
            CREATE TABLE IF NOT EXISTS Actors (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Username TEXT,
                PasswordHash TEXT,
                IsSuperUser INTEGER NOT NULL
            );

            -- Roles table
            CREATE TABLE IF NOT EXISTS Roles (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Strength INTEGER NOT NULL
            );

            -- RolePrivileges table
            CREATE TABLE IF NOT EXISTS RolePrivileges (
                RoleId TEXT NOT NULL,
                Privilege TEXT NOT NULL,
                PRIMARY KEY (RoleId, Privilege),
                FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
            );

            -- TaskResourceUsages table
            CREATE TABLE IF NOT EXISTS TaskResourceUsages (
                NodeId TEXT NOT NULL,
                ResourceId TEXT NOT NULL,
                Amount REAL NOT NULL,
                PRIMARY KEY (NodeId, ResourceId),
                FOREIGN KEY (NodeId) REFERENCES Nodes(Id) ON DELETE CASCADE,
                FOREIGN KEY (ResourceId) REFERENCES Resources(Id) ON DELETE CASCADE
            );

            -- Settings table for storing selected graph and project
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT
            );

            -- Create indexes for better performance
            CREATE INDEX IF NOT EXISTS idx_nodes_graphid ON Nodes(GraphId);
            CREATE INDEX IF NOT EXISTS idx_edges_graphid ON Edges(GraphId);
            CREATE INDEX IF NOT EXISTS idx_projects_graphid ON Projects(GraphId);
            CREATE INDEX IF NOT EXISTS idx_taskresourceusages_nodeid ON TaskResourceUsages(NodeId);
            CREATE INDEX IF NOT EXISTS idx_taskresourceusages_resourceid ON TaskResourceUsages(ResourceId);
        ";

        createTablesCommand.ExecuteNonQuery();
        Console.WriteLine("[SqliteSaveService] Database initialized successfully");
    }

    public void Save(SaveData data)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                
                ClearAllTables(connection, transaction);

                
                SaveResources(connection, transaction, data.Resources);
                SaveActors(connection, transaction, data.Actors);
                SaveRoles(connection, transaction, data.Roles);

                
                SaveGraphs(connection, transaction, data.Graphs);

                
                SaveProjects(connection, transaction, data.Projects);

                
                SaveSettings(connection, transaction, data.SelectedGraphId, data.SelectedProjectId);

                transaction.Commit();
                Console.WriteLine($"[SqliteSaveService] Data successfully saved to: {_databasePath}");
                
                
                int totalResourceUsages = 0;
                foreach (var graph in data.Graphs)
                {
                    foreach (var node in graph.Nodes)
                    {
                        totalResourceUsages += node.ResourceUsages?.Count ?? 0;
                    }
                }
                Console.WriteLine($"[SqliteSaveService] Saved resource usages: {totalResourceUsages}");
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SqliteSaveService] Error saving data: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public SaveData? Load()
    {
        try
        {
            if (!File.Exists(_databasePath))
            {
                Console.WriteLine($"[SqliteSaveService] Database file not found: {_databasePath}");
                return null;
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var saveData = new SaveData
            {
                Graphs = LoadGraphs(connection),
                Projects = LoadProjects(connection),
                Resources = LoadResources(connection),
                Actors = LoadActors(connection),
                Roles = LoadRoles(connection)
            };

            
            LoadSettings(connection, saveData);

            Console.WriteLine($"[SqliteSaveService] Data successfully loaded from: {_databasePath}");
            
            
            int totalResourceUsages = 0;
            foreach (var graph in saveData.Graphs)
            {
                foreach (var node in graph.Nodes)
                {
                    totalResourceUsages += node.ResourceUsages?.Count ?? 0;
                }
            }
            Console.WriteLine($"[SqliteSaveService] Loaded resource usages: {totalResourceUsages}");

            return saveData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SqliteSaveService] Error loading data: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    #region Save Methods

    private void ClearAllTables(SqliteConnection connection, SqliteTransaction transaction)
    {
        var tables = new[] { "TaskResourceUsages", "RolePrivileges", "ProjectActorRoles", "Edges", "Nodes", "Projects", "Graphs", "Resources", "Actors", "Roles", "Settings" };
        
        foreach (var table in tables)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"DELETE FROM {table}";
            command.ExecuteNonQuery();
        }
    }

    private void SaveGraphs(SqliteConnection connection, SqliteTransaction transaction, List<GraphSaveData> graphs)
    {
        foreach (var graph in graphs)
        {
            
            var graphCommand = connection.CreateCommand();
            graphCommand.Transaction = transaction;
            graphCommand.CommandText = "INSERT INTO Graphs (Id, Name) VALUES (@Id, @Name)";
            graphCommand.Parameters.AddWithValue("@Id", graph.Id);
            graphCommand.Parameters.AddWithValue("@Name", graph.Name);
            graphCommand.ExecuteNonQuery();

            
            foreach (var node in graph.Nodes)
            {
                var nodeCommand = connection.CreateCommand();
                nodeCommand.Transaction = transaction;
                nodeCommand.CommandText = @"
                    INSERT INTO Nodes (Id, GraphId, Label, X, Y, TaskId, Title, Status, Priority, Payload, Deadline, ActorId, IsCompleted)
                    VALUES (@Id, @GraphId, @Label, @X, @Y, @TaskId, @Title, @Status, @Priority, @Payload, @Deadline, @ActorId, @IsCompleted)
                ";
                nodeCommand.Parameters.AddWithValue("@Id", node.Id);
                nodeCommand.Parameters.AddWithValue("@GraphId", graph.Id);
                nodeCommand.Parameters.AddWithValue("@Label", node.Label);
                nodeCommand.Parameters.AddWithValue("@X", node.X);
                nodeCommand.Parameters.AddWithValue("@Y", node.Y);
                nodeCommand.Parameters.AddWithValue("@TaskId", (object?)node.TaskId ?? DBNull.Value);
                nodeCommand.Parameters.AddWithValue("@Title", node.Title);
                nodeCommand.Parameters.AddWithValue("@Status", node.Status);
                nodeCommand.Parameters.AddWithValue("@Priority", node.Priority);
                nodeCommand.Parameters.AddWithValue("@Payload", node.Payload);
                nodeCommand.Parameters.AddWithValue("@Deadline", node.Deadline?.ToString("o") ?? (object)DBNull.Value);
                nodeCommand.Parameters.AddWithValue("@ActorId", (object?)node.ActorId ?? DBNull.Value);
                nodeCommand.Parameters.AddWithValue("@IsCompleted", node.IsCompleted ? 1 : 0);
                nodeCommand.ExecuteNonQuery();

                
                foreach (var resourceUsage in node.ResourceUsages)
                {
                    var resourceUsageCommand = connection.CreateCommand();
                    resourceUsageCommand.Transaction = transaction;
                    resourceUsageCommand.CommandText = @"
                        INSERT INTO TaskResourceUsages (NodeId, ResourceId, Amount)
                        VALUES (@NodeId, @ResourceId, @Amount)
                    ";
                    resourceUsageCommand.Parameters.AddWithValue("@NodeId", node.Id);
                    resourceUsageCommand.Parameters.AddWithValue("@ResourceId", resourceUsage.ResourceId);
                    resourceUsageCommand.Parameters.AddWithValue("@Amount", resourceUsage.Amount);
                    resourceUsageCommand.ExecuteNonQuery();
                }
            }

            
            foreach (var edge in graph.Edges)
            {
                var edgeCommand = connection.CreateCommand();
                edgeCommand.Transaction = transaction;
                edgeCommand.CommandText = @"
                    INSERT INTO Edges (GraphId, SourceId, TargetId, EdgeType)
                    VALUES (@GraphId, @SourceId, @TargetId, @EdgeType)
                ";
                edgeCommand.Parameters.AddWithValue("@GraphId", graph.Id);
                edgeCommand.Parameters.AddWithValue("@SourceId", edge.SourceId);
                edgeCommand.Parameters.AddWithValue("@TargetId", edge.TargetId);
                edgeCommand.Parameters.AddWithValue("@EdgeType", edge.EdgeType);
                edgeCommand.ExecuteNonQuery();
            }
        }
    }

    private void SaveProjects(SqliteConnection connection, SqliteTransaction transaction, List<ProjectSaveData> projects)
    {
        foreach (var project in projects)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO Projects (Id, Name, GraphId)
                VALUES (@Id, @Name, @GraphId)
            ";
            command.Parameters.AddWithValue("@Id", project.Id);
            command.Parameters.AddWithValue("@Name", project.Name);
            command.Parameters.AddWithValue("@GraphId", (object?)project.GraphId ?? DBNull.Value);
            command.ExecuteNonQuery();

            
            foreach (var actorRole in project.ActorRoles)
            {
                var actorRoleCommand = connection.CreateCommand();
                actorRoleCommand.Transaction = transaction;
                actorRoleCommand.CommandText = @"
                    INSERT INTO ProjectActorRoles (ProjectId, ActorId, RoleId)
                    VALUES (@ProjectId, @ActorId, @RoleId)
                ";
                actorRoleCommand.Parameters.AddWithValue("@ProjectId", project.Id);
                actorRoleCommand.Parameters.AddWithValue("@ActorId", actorRole.ActorId);
                actorRoleCommand.Parameters.AddWithValue("@RoleId", actorRole.RoleId);
                actorRoleCommand.ExecuteNonQuery();
            }
        }
    }

    private void SaveResources(SqliteConnection connection, SqliteTransaction transaction, List<ResourceSaveData> resources)
    {
        foreach (var resource in resources)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO Resources (Id, Name, Value)
                VALUES (@Id, @Name, @Value)
            ";
            command.Parameters.AddWithValue("@Id", resource.Id);
            command.Parameters.AddWithValue("@Name", resource.Name);
            command.Parameters.AddWithValue("@Value", resource.Value);
            command.ExecuteNonQuery();
        }
    }

    private void SaveActors(SqliteConnection connection, SqliteTransaction transaction, List<ActorSaveData> actors)
    {
        foreach (var actor in actors)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO Actors (Id, Name, Username, PasswordHash, IsSuperUser)
                VALUES (@Id, @Name, @Username, @PasswordHash, @IsSuperUser)
            ";
            command.Parameters.AddWithValue("@Id", actor.Id);
            command.Parameters.AddWithValue("@Name", actor.Name);
            command.Parameters.AddWithValue("@Username", (object?)actor.Username ?? DBNull.Value);
            command.Parameters.AddWithValue("@PasswordHash", (object?)actor.PasswordHash ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsSuperUser", actor.IsSuperUser ? 1 : 0);
            command.ExecuteNonQuery();
        }
    }

    private void SaveRoles(SqliteConnection connection, SqliteTransaction transaction, List<RoleSaveData> roles)
    {
        foreach (var role in roles)
        {
            
            var roleCommand = connection.CreateCommand();
            roleCommand.Transaction = transaction;
            roleCommand.CommandText = @"
                INSERT INTO Roles (Id, Name, Strength)
                VALUES (@Id, @Name, @Strength)
            ";
            roleCommand.Parameters.AddWithValue("@Id", role.Id);
            roleCommand.Parameters.AddWithValue("@Name", role.Name);
            roleCommand.Parameters.AddWithValue("@Strength", role.Strength);
            roleCommand.ExecuteNonQuery();

            
            foreach (var privilege in role.Privileges)
            {
                var privilegeCommand = connection.CreateCommand();
                privilegeCommand.Transaction = transaction;
                privilegeCommand.CommandText = @"
                    INSERT INTO RolePrivileges (RoleId, Privilege)
                    VALUES (@RoleId, @Privilege)
                ";
                privilegeCommand.Parameters.AddWithValue("@RoleId", role.Id);
                privilegeCommand.Parameters.AddWithValue("@Privilege", privilege.ToString());
                privilegeCommand.ExecuteNonQuery();
            }
        }
    }

    private void SaveSettings(SqliteConnection connection, SqliteTransaction transaction, string? selectedGraphId, string? selectedProjectId)
    {
        if (selectedGraphId != null)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Settings (Key, Value) VALUES (@Key, @Value)";
            command.Parameters.AddWithValue("@Key", "SelectedGraphId");
            command.Parameters.AddWithValue("@Value", selectedGraphId);
            command.ExecuteNonQuery();
        }

        if (selectedProjectId != null)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Settings (Key, Value) VALUES (@Key, @Value)";
            command.Parameters.AddWithValue("@Key", "SelectedProjectId");
            command.Parameters.AddWithValue("@Value", selectedProjectId);
            command.ExecuteNonQuery();
        }
    }

    #endregion

    #region Load Methods

    private List<GraphSaveData> LoadGraphs(SqliteConnection connection)
    {
        var graphs = new List<GraphSaveData>();

        var graphCommand = connection.CreateCommand();
        graphCommand.CommandText = "SELECT Id, Name FROM Graphs";

        using var graphReader = graphCommand.ExecuteReader();
        while (graphReader.Read())
        {
            var graph = new GraphSaveData
            {
                Id = graphReader.GetString(0),
                Name = graphReader.GetString(1),
                Nodes = new List<NodeSaveData>(),
                Edges = new List<EdgeSaveData>()
            };

            graphs.Add(graph);
        }
        graphReader.Close();

        
        foreach (var graph in graphs)
        {
            var nodeCommand = connection.CreateCommand();
            nodeCommand.CommandText = @"
                SELECT Id, Label, X, Y, TaskId, Title, Status, Priority, Payload, Deadline, ActorId, IsCompleted
                FROM Nodes
                WHERE GraphId = @GraphId
            ";
            nodeCommand.Parameters.AddWithValue("@GraphId", graph.Id);

            using var nodeReader = nodeCommand.ExecuteReader();
            while (nodeReader.Read())
            {
                var node = new NodeSaveData
                {
                    Id = nodeReader.GetString(0),
                    Label = nodeReader.GetString(1),
                    X = nodeReader.GetDouble(2),
                    Y = nodeReader.GetDouble(3),
                    TaskId = nodeReader.IsDBNull(4) ? null : nodeReader.GetString(4),
                    Title = nodeReader.GetString(5),
                    Status = nodeReader.GetString(6),
                    Priority = nodeReader.GetInt32(7),
                    Payload = nodeReader.GetString(8),
                    Deadline = nodeReader.IsDBNull(9) ? null : DateTimeOffset.Parse(nodeReader.GetString(9)),
                    ActorId = nodeReader.IsDBNull(10) ? null : nodeReader.GetString(10),
                    IsCompleted = nodeReader.GetInt32(11) == 1,
                    ResourceUsages = new List<TaskResourceUsageSaveData>()
                };

                graph.Nodes.Add(node);
            }
            nodeReader.Close();

            
            foreach (var node in graph.Nodes)
            {
                var resourceUsageCommand = connection.CreateCommand();
                resourceUsageCommand.CommandText = @"
                    SELECT ResourceId, Amount
                    FROM TaskResourceUsages
                    WHERE NodeId = @NodeId
                ";
                resourceUsageCommand.Parameters.AddWithValue("@NodeId", node.Id);

                using var resourceUsageReader = resourceUsageCommand.ExecuteReader();
                while (resourceUsageReader.Read())
                {
                    node.ResourceUsages.Add(new TaskResourceUsageSaveData
                    {
                        ResourceId = resourceUsageReader.GetString(0),
                        Amount = resourceUsageReader.GetDouble(1)
                    });
                }
            }

            
            var edgeCommand = connection.CreateCommand();
            edgeCommand.CommandText = @"
                SELECT SourceId, TargetId, EdgeType
                FROM Edges
                WHERE GraphId = @GraphId
            ";
            edgeCommand.Parameters.AddWithValue("@GraphId", graph.Id);

            using var edgeReader = edgeCommand.ExecuteReader();
            while (edgeReader.Read())
            {
                graph.Edges.Add(new EdgeSaveData
                {
                    SourceId = edgeReader.GetString(0),
                    TargetId = edgeReader.GetString(1),
                    EdgeType = edgeReader.GetString(2)
                });
            }
        }

        return graphs;
    }

    private List<ProjectSaveData> LoadProjects(SqliteConnection connection)
    {
        var projects = new List<ProjectSaveData>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, GraphId FROM Projects";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            projects.Add(new ProjectSaveData
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                GraphId = reader.IsDBNull(2) ? null : reader.GetString(2),
                ActorRoles = new List<ProjectActorRoleSaveData>()
            });
        }
        reader.Close();

        
        foreach (var project in projects)
        {
            var actorRoleCommand = connection.CreateCommand();
            actorRoleCommand.CommandText = @"
                SELECT ActorId, RoleId
                FROM ProjectActorRoles
                WHERE ProjectId = @ProjectId
            ";
            actorRoleCommand.Parameters.AddWithValue("@ProjectId", project.Id);

            using var actorRoleReader = actorRoleCommand.ExecuteReader();
            while (actorRoleReader.Read())
            {
                project.ActorRoles.Add(new ProjectActorRoleSaveData
                {
                    ActorId = actorRoleReader.GetString(0),
                    RoleId = actorRoleReader.GetString(1)
                });
            }
        }

        return projects;
    }

    private List<ResourceSaveData> LoadResources(SqliteConnection connection)
    {
        var resources = new List<ResourceSaveData>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Value FROM Resources";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            resources.Add(new ResourceSaveData
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Value = reader.GetDouble(2)
            });
        }

        return resources;
    }

    private List<ActorSaveData> LoadActors(SqliteConnection connection)
    {
        var actors = new List<ActorSaveData>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Username, PasswordHash, IsSuperUser FROM Actors";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            actors.Add(new ActorSaveData
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Username = reader.IsDBNull(2) ? null : reader.GetString(2),
                PasswordHash = reader.IsDBNull(3) ? null : reader.GetString(3),
                IsSuperUser = reader.GetInt32(4) == 1
            });
        }

        return actors;
    }

    private List<RoleSaveData> LoadRoles(SqliteConnection connection)
    {
        var roles = new List<RoleSaveData>();

        var roleCommand = connection.CreateCommand();
        roleCommand.CommandText = "SELECT Id, Name, Strength FROM Roles";

        using var roleReader = roleCommand.ExecuteReader();
        while (roleReader.Read())
        {
            var role = new RoleSaveData
            {
                Id = roleReader.GetString(0),
                Name = roleReader.GetString(1),
                Strength = roleReader.GetInt32(2),
                Privileges = new List<RolePriv>()
            };

            roles.Add(role);
        }
        roleReader.Close();

        
        foreach (var role in roles)
        {
            var privilegeCommand = connection.CreateCommand();
            privilegeCommand.CommandText = "SELECT Privilege FROM RolePrivileges WHERE RoleId = @RoleId";
            privilegeCommand.Parameters.AddWithValue("@RoleId", role.Id);

            using var privilegeReader = privilegeCommand.ExecuteReader();
            while (privilegeReader.Read())
            {
                if (Enum.TryParse<RolePriv>(privilegeReader.GetString(0), out var privilege))
                {
                    role.Privileges.Add(privilege);
                }
            }
        }

        return roles;
    }

    private void LoadSettings(SqliteConnection connection, SaveData saveData)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Key, Value FROM Settings";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var key = reader.GetString(0);
            var value = reader.GetString(1);

            switch (key)
            {
                case "SelectedGraphId":
                    saveData.SelectedGraphId = value;
                    break;
                case "SelectedProjectId":
                    saveData.SelectedProjectId = value;
                    break;
            }
        }
    }

    #endregion
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using YoteiLib.Core;
using YoteiTasks.Adapters;
using YoteiTasks.Models;
using YoteiTasks.Services;

namespace YoteiTasks.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public enum ToolbarTab
    {
        Projects,
        Resources,
        Actors,
        Roles
    }

    private readonly Yotei _yotei;
    private readonly ISaveService _saveService;
    private readonly IUserActorService _userActorService;
    private readonly ISuperUserService _superUserService;
    private readonly ProjectPermissionService _permissionService;
    private readonly NotificationService _notificationService;
    private ObservableCollection<Graph> _graphs;
    private Graph? _selectedGraph;
    private bool _suppressUpdates = false;
    private GraphNode? _selectedNode;
    private TaskEditorViewModel? _taskEditor;
    private Timer? _autoSaveTimer;
    private ObservableCollection<ResourceViewModel> _resources = new();
    private ObservableCollection<ActorViewModel> _actors = new();
    private ObservableCollection<RoleViewModel> _roles = new();
    private ObservableCollection<Project> _projects = new();
    private Project? _selectedProject;
    private ToolbarTab _selectedTab = ToolbarTab.Projects;
    private bool _isLoginPopupOpen;
    private bool _isProjectCreationPopupOpen;
    private bool _isProjectEditorPopupOpen;
    private Actor? _currentActor;
    private ProjectPermissions _currentPermissions = new();

    public ObservableCollection<Graph> Graphs
    {
        get => _graphs;
        set => SetProperty(ref _graphs, value);
    }

    public Graph? SelectedGraph
    {
        get => _selectedGraph;
        set
        {
            if (SetProperty(ref _selectedGraph, value))
            {
                OnPropertyChanged(nameof(HasSelectedGraph));
            }
        }
    }

    public GraphNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                if (value != null && SelectedGraph != null)
                {
                    TaskEditor = new TaskEditorViewModel(value, this, SelectedGraph);
                }
                else
                {
                    TaskEditor = null;
                }
            }
        }
    }

    public TaskEditorViewModel? TaskEditor
    {
        get => _taskEditor;
        private set
        {
            if (SetProperty(ref _taskEditor, value))
            {
                OnPropertyChanged(nameof(HasTaskEditor));
            }
        }
    }

    public ObservableCollection<ResourceViewModel> Resources
    {
        get => _resources;
        set => SetProperty(ref _resources, value);
    }

    public ObservableCollection<ActorViewModel> Actors
    {
        get => _actors;
        set => SetProperty(ref _actors, value);
    }

    public ObservableCollection<RoleViewModel> Roles
    {
        get => _roles;
        set => SetProperty(ref _roles, value);
    }

    public ObservableCollection<Project> Projects
    {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    public Project? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value))
            {
                Console.WriteLine($"[MainViewModel] SelectedProject изменён на: {value?.Name ?? "null"} (ID: {value?.Id ?? "null"})");
                
                if (value != null)
                {
                    SelectedGraph = value.Graph;
                }
                UpdatePermissions();
                (EditProjectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteProjectCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ToolbarTab SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value))
            {
                OnPropertyChanged(nameof(IsProjectsTab));
                OnPropertyChanged(nameof(IsResourcesTab));
                OnPropertyChanged(nameof(IsActorsTab));
                OnPropertyChanged(nameof(IsRolesTab));
            }
        }
    }

    public bool IsProjectsTab => SelectedTab == ToolbarTab.Projects;
    public bool IsResourcesTab => SelectedTab == ToolbarTab.Resources;
    public bool IsActorsTab => SelectedTab == ToolbarTab.Actors;
    public bool IsRolesTab => SelectedTab == ToolbarTab.Roles;

    public bool HasSelectedGraph => SelectedGraph != null;
    public bool HasTaskEditor => TaskEditor != null;

    public bool IsLoginPopupOpen
    {
        get => _isLoginPopupOpen;
        set => SetProperty(ref _isLoginPopupOpen, value);
    }

    public bool IsProjectCreationPopupOpen
    {
        get => _isProjectCreationPopupOpen;
        set => SetProperty(ref _isProjectCreationPopupOpen, value);
    }

    public bool IsProjectEditorPopupOpen
    {
        get => _isProjectEditorPopupOpen;
        set => SetProperty(ref _isProjectEditorPopupOpen, value);
    }

    public Actor? CurrentActor
    {
        get => _currentActor;
        private set
        {
            if (SetProperty(ref _currentActor, value))
            {
                OnPropertyChanged(nameof(IsLoggedIn));
                OnPropertyChanged(nameof(CurrentActorName));
                OnPropertyChanged(nameof(IsSuperUser));
                OnPropertyChanged(nameof(CanCreateActors));
                OnPropertyChanged(nameof(CanCreateRoles));
                OnPropertyChanged(nameof(CanCreateProjects));
                (EditProjectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteProjectCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsLoggedIn => CurrentActor != null;
    public string CurrentActorName => CurrentActor?.Name ?? "Не выполнен вход";

    public ProjectPermissions CurrentPermissions
    {
        get => _currentPermissions;
        private set
        {
            if (SetProperty(ref _currentPermissions, value))
            {
                OnPropertyChanged(nameof(CanViewProject));
                OnPropertyChanged(nameof(CanEditProject));
                OnPropertyChanged(nameof(CanCreateTasks));
                OnPropertyChanged(nameof(CanEditTasks));
                OnPropertyChanged(nameof(CanDeleteTasks));
                OnPropertyChanged(nameof(IsReadOnlyMode));
            }
        }
    }

    
    public bool CanViewProject => _currentPermissions.CanViewProject;
    public bool CanEditProject => _currentPermissions.CanEditProject;
    public bool CanCreateTasks => _currentPermissions.CanCreateTasks;
    public bool CanEditTasks => _currentPermissions.CanEditTasks;
    public bool CanDeleteTasks => _currentPermissions.CanDeleteTasks;
    public bool IsReadOnlyMode => _currentPermissions.IsReadOnly;

    public Yotei Yotei => _yotei;
    public IUserActorService UserActorService => _userActorService;
    public ISuperUserService SuperUserService => _superUserService;
    
    
    public bool IsSuperUser => _superUserService.IsSuperUser(_currentActor);
    public bool CanCreateActors => IsSuperUser;
    public bool CanCreateRoles => IsSuperUser;
    public bool CanCreateProjects => IsSuperUser;

    public ICommand AddGraphCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ShowProjectsCommand { get; }
    public ICommand ShowResourcesCommand { get; }
    public ICommand ShowActorsCommand { get; }
    public ICommand ShowRolesCommand { get; }
    public ICommand AddResourceCommand { get; }
    public ICommand AddActorCommand { get; }
    public ICommand AddRoleCommand { get; }
    public ICommand ShowLoginCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand ShowProjectCreationCommand { get; }
    public ICommand EditProjectCommand { get; }
    public ICommand DeleteProjectCommand { get; }

    public bool SuppressUpdates
    {
        get => _suppressUpdates;
        set => _suppressUpdates = value;
    }

    public MainViewModel(Yotei yotei, ISaveService? saveService = null, IUserActorService? userActorService = null, ISuperUserService? superUserService = null)
    {
        _yotei = yotei;
        _saveService = saveService ?? new JsonSaveService();
        _userActorService = userActorService ?? new UserActorService(_yotei.Actors);
        _superUserService = superUserService ?? new SuperUserService(_yotei, _userActorService);
        _permissionService = new ProjectPermissionService(_yotei, _superUserService);
        _notificationService = NotificationService.Instance;
        _graphs = new ObservableCollection<Graph>();
        AddGraphCommand = new RelayCommand(_ => AddGraph());
        SaveCommand = new RelayCommand(_ => Save());
        ShowProjectsCommand = new RelayCommand(_ => SelectedTab = ToolbarTab.Projects);
        ShowResourcesCommand = new RelayCommand(_ => SelectedTab = ToolbarTab.Resources);
        ShowActorsCommand = new RelayCommand(_ => SelectedTab = ToolbarTab.Actors);
        ShowRolesCommand = new RelayCommand(_ => SelectedTab = ToolbarTab.Roles);
        AddResourceCommand = new RelayCommand(_ => AddResource());
        AddActorCommand = new RelayCommand(_ => AddActor());
        AddRoleCommand = new RelayCommand(_ => AddRole());
        ShowLoginCommand = new RelayCommand(_ => ShowLogin());
        LogoutCommand = new RelayCommand(_ => Logout());
        ShowProjectCreationCommand = new RelayCommand(_ => ShowProjectCreation());
        EditProjectCommand = new RelayCommand(_ => EditProject(), _ => CanEditSelectedProject());
        DeleteProjectCommand = new RelayCommand(_ => DeleteProject(), _ => CanDeleteSelectedProject());
        
        _yotei.Tasks.OnUpdate += OnTasksUpdated;

        LoadResourcesAndActors();
        LoadRoles();
        Load();
        
        
        _superUserService.GetOrCreateSuperUser();
        
        if (_graphs.Count == 0)
        {
            LoadGraphFromRepository();
        }
        
        StartAutoSave();
    }

    private void LoadGraphFromRepository()
    {
        var graph = YoteiAdapter.FromTaskRepository(_yotei.Tasks, "Задачи Yotei");
        _graphs.Add(graph);
        _selectedGraph = graph;
    }

    private void LoadResourcesAndActors()
    {
        var resources = new ObservableCollection<ResourceViewModel>();
        foreach (var resource in _yotei.Resources.GetAll())
        {
            resources.Add(new ResourceViewModel(resource, DeleteResource));
        }
        Resources = resources;

        var actors = new ObservableCollection<ActorViewModel>();
        foreach (var actor in _yotei.Actors)
        {
            actors.Add(new ActorViewModel(actor, DeleteActor, _userActorService));
        }
        Actors = actors;
    }

    private void LoadRoles()
    {
        var roles = new ObservableCollection<RoleViewModel>();
        foreach (var role in _yotei.Roles)
        {
            roles.Add(new RoleViewModel(role, DeleteRole));
        }
        Roles = roles;
    }

    private void OnTasksUpdated()
    {
        if (_suppressUpdates || _selectedGraph == null)
            return;
        
     
        
        var allTasks = _yotei.Tasks.GetAll();
        var taskIdToNode = _selectedGraph.Nodes
            .Where(n => n.TaskNode != null)
            .ToDictionary(n => n.TaskNode!.Id.ToString(), n => n);
        
       
        var edgesToAdd = new List<GraphEdge>();
        var existingEdgeKeys = new HashSet<string>(
            _selectedGraph.Edges.Select(e => $"{e.SourceId}-{e.TargetId}"));
        
        foreach (var node in _selectedGraph.Nodes)
        {
            if (node.TaskNode != null && taskIdToNode.ContainsKey(node.TaskNode.Id.ToString()))
            {
                var outgoing = _yotei.Tasks.GetOutgoing(node.TaskNode);
                foreach (var targetTask in outgoing)
                {
                    var targetId = targetTask.Id.ToString();
                    if (taskIdToNode.ContainsKey(targetId))
                    {
                        var edgeKey = $"{node.Id}-{targetId}";
                        if (!existingEdgeKeys.Contains(edgeKey))
                        {
                            edgesToAdd.Add(new GraphEdge(node.Id, targetId));
                        }
                    }
                }
            }
        }
        
       
        foreach (var edge in edgesToAdd)
        {
            _selectedGraph.Edges.Add(edge);
        }
        
    
        foreach (var node in _selectedGraph.Nodes)
        {
            if (node.TaskNode != null)
            {
                node.SyncFromTaskNode();
                node.RaiseVisualChanged();
            }
        }
    }

    public void AddGraph()
    {
        ShowProjectCreation();
    }

    private void ShowProjectCreation()
    {
        if (!IsSuperUser)
        {
            _notificationService.ShowError("Только SuperUser может создавать новые проекты");
            return;
        }
        
        IsProjectCreationPopupOpen = true;
    }

    public void OnProjectCreated(Project? project)
    {
        IsProjectCreationPopupOpen = false;
        
        if (project != null)
        {
            _projects.Add(project);
            _graphs.Add(project.Graph);
            SelectedProject = project;
            SelectedGraph = project.Graph;
            _notificationService.ShowSuccess($"Проект '{project.Name}' успешно создан");
        }
    }

    private bool CanEditSelectedProject()
    {
        return IsSuperUser && SelectedProject != null;
    }

    private void EditProject()
    {
        if (!IsSuperUser)
        {
            _notificationService.ShowError("Только SuperUser может редактировать проекты");
            return;
        }

        if (SelectedProject == null)
        {
            _notificationService.ShowError("Выберите проект для редактирования");
            return;
        }

        IsProjectEditorPopupOpen = true;
    }

    public void OnProjectEdited(bool saved)
    {
        IsProjectEditorPopupOpen = false;
        
        if (saved && SelectedProject != null)
        {
            
            if (SelectedProject.Graph.Name != SelectedProject.Name)
            {
                SelectedProject.Graph.Name = SelectedProject.Name;
            }
            
            UpdatePermissions();
            _notificationService.ShowSuccess($"Проект '{SelectedProject.Name}' успешно обновлён");
        }
    }

    private bool CanDeleteSelectedProject()
    {
        return IsSuperUser && SelectedProject != null;
    }

    private void DeleteProject()
    {
        if (!IsSuperUser)
        {
            _notificationService.ShowError("Только SuperUser может удалять проекты");
            return;
        }

        if (SelectedProject == null)
        {
            _notificationService.ShowError("Выберите проект для удаления");
            return;
        }

        var projectName = SelectedProject.Name;
        var projectGraph = SelectedProject.Graph;
        
        
        _projects.Remove(SelectedProject);
        
        
        _graphs.Remove(projectGraph);
        
        
        SelectedProject = null;
        SelectedGraph = null;
        
        _notificationService.ShowSuccess($"Проект '{projectName}' успешно удалён");
    }

    private void AddResource()
    {
        var resource = _yotei.Resources.Create();
        Resources.Add(new ResourceViewModel(resource, DeleteResource));
    }

    private void DeleteResource(ResourceViewModel resourceViewModel)
    {
        _yotei.Resources.Delete(resourceViewModel.Model);
        Resources.Remove(resourceViewModel);
    }

    private void AddActor()
    {
        if (!IsSuperUser)
        {
            _notificationService.ShowError("Только SuperUser может создавать новых акторов");
            return;
        }
        
        var actor = _yotei.Actors.Create();
        Actors.Add(new ActorViewModel(actor, DeleteActor, _userActorService));
    }

    private void AddRole()
    {
        if (!IsSuperUser)
        {
            _notificationService.ShowError("Только SuperUser может создавать новые роли");
            return;
        }
        
        var role = _yotei.Roles.Create();
        Roles.Add(new RoleViewModel(role, DeleteRole));
    }

    private void ShowLogin()
    {
        IsLoginPopupOpen = true;
    }

    public void OnLoginCompleted(Actor? actor)
    {
        IsLoginPopupOpen = false;
        if (actor != null)
        {
            CurrentActor = actor;
            UpdatePermissions();
            _notificationService.ShowSuccess($"Добро пожаловать, {actor.Name}!");
        }
        else
        {
            _notificationService.ShowInfo("Вход отменён");
        }
    }

    private void Logout()
    {
        if (CurrentActor == null)
        {
            _notificationService.ShowInfo("Вы не вошли в систему");
            return;
        }
        
        var actorName = CurrentActor.Name;
        CurrentActor = null;
        UpdatePermissions();
        _notificationService.ShowInfo($"Вы вышли из аккаунта {actorName}");
    }

    private void UpdatePermissions()
    {
        CurrentPermissions = _permissionService.GetPermissions(_selectedProject, _currentActor);
    }

    private void DeleteActor(ActorViewModel actorViewModel)
    {
        _yotei.Actors.Delete(actorViewModel.Model);
        Actors.Remove(actorViewModel);
    }

    private void DeleteRole(RoleViewModel roleViewModel)
    {
        _yotei.Roles.Delete(roleViewModel.Model);
        Roles.Remove(roleViewModel);
    }

    public void SyncGraphToRepository(Graph? graph = null)
    {
        var graphToSync = graph ?? _selectedGraph;
        if (graphToSync != null)
        {
            YoteiAdapter.SyncGraphToRepository(graphToSync, _yotei.Tasks);
        }
    }
    public GraphNode CreateTaskNode(string title, double x, double y)
    {
        return YoteiAdapter.CreateTaskNode(_yotei.Tasks, title, x, y);
    }


    public Result DeleteTaskNode(GraphNode node)
    {
        return YoteiAdapter.DeleteTaskNode(node, _yotei.Tasks);
    }

    
    public void AddRelation(GraphNode fromNode, GraphNode toNode, EdgeType edgeType = EdgeType.Block)
    {
        YoteiAdapter.AddRelation(fromNode, toNode, _yotei.Tasks, edgeType);
        
      
        if (_selectedGraph != null)
        {
            var edgeExists = _selectedGraph.Edges.Any(e => 
                e.SourceId == fromNode.Id && e.TargetId == toNode.Id);
            
            if (!edgeExists)
            {
                var edge = new GraphEdge(fromNode.Id, toNode.Id);
                _selectedGraph.Edges.Add(edge);
            }
        }
    }

    public void RemoveGraph(Graph graph)
    {
        if (graph == SelectedGraph)
        {
            SelectedGraph = null;
        }
        Graphs.Remove(graph);
    }


    public void Save()
    {
        try
        {
            var resources = _resources.Select(vm => vm.Model).ToList();
            var actors = _actors.Select(vm => vm.Model).ToList();
            var roles = _yotei.Roles.GetAll();
            var saveData = SaveDataConverter.ToSaveData(_graphs, _selectedGraph, _yotei.Tasks, resources, actors, roles, _projects, _selectedProject, _superUserService);
            _saveService.Save(saveData);
            _notificationService.ShowSuccess("Данные успешно сохранены");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении: {ex}");
            _notificationService.ShowError($"Ошибка при сохранении: {ex.Message}");
        }
    }

 
    private void Load()
    {
        try
        {
            var saveData = _saveService.Load();
            if (saveData == null)
                return;

            _suppressUpdates = true;

            
            var (loadedGraphs, loadedResources, loadedActors, loadedProjects) = 
                SaveDataConverter.FromSaveData(saveData, _yotei.Tasks, _yotei, _superUserService);
            
            
            _graphs.Clear();
            foreach (var graph in loadedGraphs)
            {
                _graphs.Add(graph);
            }

            
            _resources.Clear();
            foreach (var resource in loadedResources)
            {
                _resources.Add(new ResourceViewModel(resource, DeleteResource));
            }

            
            _actors.Clear();
            foreach (var actor in loadedActors)
            {
                _actors.Add(new ActorViewModel(actor, DeleteActor, _userActorService));
            }

            
            _projects.Clear();
            foreach (var project in loadedProjects)
            {
                _projects.Add(project);
            }
       
            
            if (!string.IsNullOrEmpty(saveData.SelectedProjectId))
            {
                var projectToSelect = loadedProjects.FirstOrDefault(p => p.Id == saveData.SelectedProjectId);
                if (projectToSelect != null)
                {
                    SelectedProject = projectToSelect;
                }
            }
            
            
            if (!string.IsNullOrEmpty(saveData.SelectedGraphId))
            {
                var graphIdMap = new Dictionary<string, Graph>();
                foreach (var graph in loadedGraphs)
                {
                    var graphId = SaveDataConverter.GetGraphId(graph);
                    graphIdMap[graphId] = graph;
                }
                
                if (graphIdMap.TryGetValue(saveData.SelectedGraphId, out var graphToSelect))
                {
                    SelectedGraph = graphToSelect;
                }
                else if (loadedGraphs.Count > 0)
                {
                    SelectedGraph = loadedGraphs[0];
                }
            }
            else if (loadedGraphs.Count > 0)
            {
                SelectedGraph = loadedGraphs[0];
            }

            
            LoadRoles();

            _suppressUpdates = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке: {ex.Message}");
        }
    }

 
    private void StartAutoSave()
    {
       
        _autoSaveTimer = new Timer(_ => Save(), null, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3));
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


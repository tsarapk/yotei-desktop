using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YoteiLib.Core;
using YoteiTasks.Models;

namespace YoteiTasks.ViewModels;




public class TaskListViewModel : INotifyPropertyChanged
{
    private readonly Graph _graph;
    private readonly Action? _closeCallback;
    private readonly Action<GraphNode>? _selectTaskCallback;
    private ObservableCollection<TaskListItemViewModel> _tasks = new();
    private ObservableCollection<TaskListItemViewModel> _filteredTasks = new();
    private string _searchText = string.Empty;
    private string _statusFilter = string.Empty;
    private TaskStatus? _selectedStatus;
    private int? _selectedPriority;
    private Actor? _selectedPerformer;
    private DateTimeOffset? _dateFrom;
    private DateTimeOffset? _dateTo;
    private TaskSortBy _sortBy = TaskSortBy.Priority;
    private SortDirection _sortDirection = SortDirection.Descending;

    public TaskListViewModel(Graph graph, IEnumerable<Actor> actors, Action? closeCallback = null, Action<GraphNode>? selectTaskCallback = null)
    {
        _graph = graph;
        _closeCallback = closeCallback;
        _selectTaskCallback = selectTaskCallback;
        
        AvailableActors = new ObservableCollection<Actor>(actors);
        
        LoadTasks();
        ApplyFilters();
        
        CloseCommand = new RelayCommand(_ => _closeCallback?.Invoke());
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
        SelectTaskCommand = new RelayCommand(param => SelectTask(param as TaskListItemViewModel));
    }

    public ObservableCollection<TaskListItemViewModel> FilteredTasks
    {
        get => _filteredTasks;
        set => SetProperty(ref _filteredTasks, value);
    }

    public ObservableCollection<Actor> AvailableActors { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public TaskStatus? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                ApplyFilters();
            }
        }
    }

    public int? SelectedPriority
    {
        get => _selectedPriority;
        set
        {
            if (SetProperty(ref _selectedPriority, value))
            {
                ApplyFilters();
            }
        }
    }

    public Actor? SelectedPerformer
    {
        get => _selectedPerformer;
        set
        {
            if (SetProperty(ref _selectedPerformer, value))
            {
                ApplyFilters();
            }
        }
    }

    public DateTimeOffset? DateFrom
    {
        get => _dateFrom;
        set
        {
            if (SetProperty(ref _dateFrom, value))
            {
                ApplyFilters();
            }
        }
    }

    public DateTimeOffset? DateTo
    {
        get => _dateTo;
        set
        {
            if (SetProperty(ref _dateTo, value))
            {
                ApplyFilters();
            }
        }
    }

    public TaskSortBy SortBy
    {
        get => _sortBy;
        set
        {
            if (SetProperty(ref _sortBy, value))
            {
                ApplyFilters();
            }
        }
    }

    public SortDirection SortDirection
    {
        get => _sortDirection;
        set
        {
            if (SetProperty(ref _sortDirection, value))
            {
                ApplyFilters();
            }
        }
    }

    public int TotalTasksCount => _tasks.Count;
    public int FilteredTasksCount => _filteredTasks.Count;
    public bool HasFilters => !string.IsNullOrWhiteSpace(_searchText) || 
                              !string.IsNullOrWhiteSpace(_statusFilter) ||
                              _selectedStatus != null || 
                              _selectedPriority != null || 
                              _selectedPerformer != null || 
                              _dateFrom != null || 
                              _dateTo != null;

    public ICommand CloseCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand SelectTaskCommand { get; }

    private void LoadTasks()
    {
        _tasks.Clear();
        foreach (var node in _graph.Nodes)
        {
            if (node.TaskNode != null)
            {
                _tasks.Add(new TaskListItemViewModel(node));
            }
        }
    }

    private void ApplyFilters()
    {
        var filtered = _tasks.AsEnumerable();

        
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            filtered = filtered.Where(t => 
                t.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                (t.Payload?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        
        if (!string.IsNullOrWhiteSpace(_statusFilter))
        {
            filtered = filtered.Where(t => 
                t.StatusText.Contains(_statusFilter, StringComparison.OrdinalIgnoreCase));
        }

        
        if (_selectedStatus != null)
        {
            filtered = filtered.Where(t => t.Status == _selectedStatus.Value);
        }

        
        if (_selectedPriority != null)
        {
            filtered = filtered.Where(t => t.Priority == _selectedPriority.Value);
        }

        
        if (_selectedPerformer != null)
        {
            filtered = filtered.Where(t => t.PerformerId == _selectedPerformer.Id.ToString());
        }

        
        if (_dateFrom != null)
        {
            filtered = filtered.Where(t => t.Deadline >= _dateFrom.Value);
        }

        if (_dateTo != null)
        {
            filtered = filtered.Where(t => t.Deadline <= _dateTo.Value);
        }

        
        filtered = _sortBy switch
        {
            TaskSortBy.Name => _sortDirection == SortDirection.Ascending 
                ? filtered.OrderBy(t => t.Name) 
                : filtered.OrderByDescending(t => t.Name),
            TaskSortBy.Status => _sortDirection == SortDirection.Ascending 
                ? filtered.OrderBy(t => t.Status) 
                : filtered.OrderByDescending(t => t.Status),
            TaskSortBy.Priority => _sortDirection == SortDirection.Ascending 
                ? filtered.OrderBy(t => t.Priority) 
                : filtered.OrderByDescending(t => t.Priority),
            TaskSortBy.Deadline => _sortDirection == SortDirection.Ascending 
                ? filtered.OrderBy(t => t.Deadline) 
                : filtered.OrderByDescending(t => t.Deadline),
            _ => filtered
        };

        FilteredTasks = new ObservableCollection<TaskListItemViewModel>(filtered);
        
        OnPropertyChanged(nameof(FilteredTasksCount));
        OnPropertyChanged(nameof(HasFilters));
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        StatusFilter = string.Empty;
        SelectedStatus = null;
        SelectedPriority = null;
        SelectedPerformer = null;
        DateFrom = null;
        DateTo = null;
    }

    private void SelectTask(TaskListItemViewModel? task)
    {
        if (task != null)
        {
            _selectTaskCallback?.Invoke(task.Node);
            _closeCallback?.Invoke();
        }
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




public class TaskListItemViewModel : INotifyPropertyChanged
{
    private readonly GraphNode _node;

    public TaskListItemViewModel(GraphNode node)
    {
        _node = node;
    }

    public GraphNode Node => _node;
    public string Name => _node.Label;
    public TaskStatus Status => _node.TaskNode?.Status ?? default;
    public int Priority => _node.TaskNode?.Priority ?? 0;
    public string? Payload => _node.TaskNode?.Payload;
    public DateTimeOffset Deadline => _node.TaskNode?.Deadline ?? DateTimeOffset.MaxValue;
    public string? PerformerId => _node.TaskNode?.Meta?.PerfomedBy?.Id.ToString();
    public string? PerformerName => _node.TaskNode?.Meta?.PerfomedBy?.Name;
    public bool IsCompleted => _node.TaskNode?.IsCompleted ?? false;
    
    public string StatusText => Status.ToString();
    public string PriorityText => $"Приоритет: {Priority}";
    public string DeadlineText => Deadline == DateTimeOffset.MaxValue ? "Без срока" : Deadline.ToString("dd.MM.yyyy");
    public string PerformerText => string.IsNullOrEmpty(PerformerName) ? "Не назначен" : PerformerName;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using YoteiLib.Core;
using YoteiTasks.Adapters;
using YoteiTasks.Models;
using YoteiTasks.ViewModels;

namespace YoteiTasks.ViewModels;

public class TaskEditorViewModel : INotifyPropertyChanged
{
    private readonly GraphNode _node;
    private readonly MainViewModel? _mainViewModel;
    private readonly Graph? _currentGraph;
    
    private string _title = string.Empty;
    private TaskStatus _selectedStatus;
    private int _priority;
    private string _payload = string.Empty;
    private DateTime? _deadline;
    private Actor? _selectedActor;
    private GraphNode? _selectedRelationNode;
    private ComboBox? _relationTypeComboBox;

    public GraphNode Node => _node;
    
    public bool IsReadOnly => _mainViewModel?.IsReadOnlyMode ?? true;
    
    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
            {
                if (_node.TaskNode != null)
                {
                    _node.TaskNode.SetTitle(_title);
                }
                _node.SyncFromTaskNode();
                _node.RaiseVisualChanged();
                _mainViewModel?.SyncGraphToRepository(_currentGraph);
            }
        }
    }

    public TaskStatus SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                if (_node.TaskNode != null && _mainViewModel != null)
                {
                    
                    if (value == TaskStatus.Completed && !_node.TaskNode.IsCompleted)
                    {
                        var result = _mainViewModel.Yotei.Tasks.TryComplete(_node.TaskNode, out var uncompleted);
                        if (!result && uncompleted != null && uncompleted.Count > 0)
                        {
                            Console.WriteLine($"Нельзя завершить задачу. Незавершенные зависимости: {uncompleted.Count}");
                        }
                    }
                    else if (value != TaskStatus.Completed)
                    {
                        _node.TaskNode.SetStatusSecure(value);
                    }
                }

                _node.SyncFromTaskNode();
                _node.RaiseVisualChanged();
                _mainViewModel?.SyncGraphToRepository(_currentGraph);
                OnPropertyChanged(nameof(CompleteButtonText));
            }
        }
    }

    public int Priority
    {
        get => _priority;
        set
        {
            if (SetProperty(ref _priority, value))
            {
                if (_node.TaskNode != null)
                {
                    _node.TaskNode.SetPriority(_priority);
                }

                _node.SyncFromTaskNode();
                _node.RaiseVisualChanged();
                _mainViewModel?.SyncGraphToRepository(_currentGraph);
            }
        }
    }

    public string Payload
    {
        get => _payload;
        set
        {
            if (SetProperty(ref _payload, value))
            {
                if (_node.TaskNode != null)
                {
                    _node.TaskNode.SetPayload(_payload ?? string.Empty);
                }

                _node.SyncFromTaskNode();
                _node.RaiseVisualChanged();
                _mainViewModel?.SyncGraphToRepository(_currentGraph);
            }
        }
    }

    public DateTime? Deadline
    {
        get => _deadline;
        set
        {
            if (SetProperty(ref _deadline, value))
            {
                if (_node.TaskNode != null)
                {
                    if (value.HasValue)
                    {
                        
                    }
                    else
                    {
                        
                    }
                }

                _node.SyncFromTaskNode();
                _node.RaiseVisualChanged();
                _mainViewModel?.SyncGraphToRepository(_currentGraph);
            }
        }
    }

    public Actor? SelectedActor
    {
        get => _selectedActor;
        set
        {
            if (SetProperty(ref _selectedActor, value))
            {
                if (_node.TaskNode != null && _mainViewModel != null && value != null)
                {
                    var currentActor = _node.TaskNode.Meta?.PerfomedBy;
                    if (currentActor != value)
                    {
                        _mainViewModel.Yotei.Tasks.BindActor(_node.TaskNode, value);
                    }
                }

                _node.SyncFromTaskNode();
                _node.RaiseVisualChanged();
                _mainViewModel?.SyncGraphToRepository(_currentGraph);
            }
        }
    }

    public GraphNode? SelectedRelationNode
    {
        get => _selectedRelationNode;
        set => SetProperty(ref _selectedRelationNode, value);
    }

    public ObservableCollection<Actor> AvailableActors { get; }
    public ObservableCollection<GraphNode> AvailableNodes { get; }

    public string CompleteButtonText => _node.TaskNode?.IsCompleted == true 
        ? "Отменить выполнение" 
        : "Завершить задачу";

    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddRelationCommand { get; }
    public ICommand CompleteTaskCommand { get; }

    public TaskEditorViewModel(GraphNode node, MainViewModel? mainViewModel, Graph? currentGraph)
    {
        _node = node;
        _mainViewModel = mainViewModel;
        _currentGraph = currentGraph;

        AvailableActors = new ObservableCollection<Actor>();
        AvailableNodes = new ObservableCollection<GraphNode>();

        
        if (node.TaskNode != null)
        {
            _title = node.TaskNode.Title;
            _selectedStatus = node.TaskNode.Status;
            _priority = node.TaskNode.Priority;
            _payload = node.TaskNode.Payload ?? string.Empty;
            if (node.TaskNode.Deadline != DateTimeOffset.MaxValue)
            {
                _deadline = node.TaskNode.Deadline.Date;
            }
            _selectedActor = node.TaskNode.Meta?.PerfomedBy;
        }
        else
        {
            _title = node.Label;
        }

        
        if (mainViewModel != null)
        {
            foreach (var actor in mainViewModel.Yotei.Actors)
            {
                AvailableActors.Add(actor);
            }
        }

        
        if (currentGraph != null)
        {
            foreach (var availableNode in currentGraph.AvaibleNodes(node))
            {
                AvailableNodes.Add(availableNode);
            }
        }

        AddRelationCommand = new RelayCommand(_ => AddRelation());
        CompleteTaskCommand = new RelayCommand(_ => ToggleComplete());
    }

    private void AddRelation()
    {
        if (_selectedRelationNode == null || _node.TaskNode == null || _mainViewModel == null)
            return;

        var edgeType = _relationTypeComboBox?.SelectedItem as EdgeType? ?? EdgeType.Block;

        _mainViewModel.AddRelation(_node, _selectedRelationNode, edgeType);

        
        AvailableNodes.Clear();
        if (_currentGraph != null)
        {
            foreach (var availableNode in _currentGraph.AvaibleNodes(_node))
            {
                AvailableNodes.Add(availableNode);
            }
        }

        _selectedRelationNode = null;
        OnPropertyChanged(nameof(SelectedRelationNode));
    }

    private void ToggleComplete()
    {
        if (_node.TaskNode == null || _mainViewModel == null)
            return;

        if (_node.TaskNode.IsCompleted)
        {
            _mainViewModel.Yotei.Tasks.Uncomplete(_node.TaskNode.Id);
        }
        else
        {
            var result = _mainViewModel.Yotei.Tasks.TryComplete(_node.TaskNode, out var uncompleted);
            if (!result && uncompleted != null && uncompleted.Count > 0)
            {
                Console.WriteLine($"Нельзя завершить задачу. Незавершенные зависимости: {uncompleted.Count}");
            }
        }

        OnPropertyChanged(nameof(CompleteButtonText));
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


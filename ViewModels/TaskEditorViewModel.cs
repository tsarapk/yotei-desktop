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
    private DateTimeOffset? _deadline;
    private Actor? _selectedActor;
    private GraphNode? _selectedRelationNode;
    private ComboBox? _relationTypeComboBox;
    private Resource? _selectedResource;
    private double _resourceAmount;
    private ObservableCollection<TaskResourceUsageViewModel> _resourceUsages = new();

    public GraphNode Node => _node;
    
    public bool IsReadOnly => _mainViewModel?.IsReadOnlyMode ?? true;
    public bool IsTaskLocked => _node.TaskNode?.IsCompleted == true;
    public bool IsEditorReadOnly => IsReadOnly || IsTaskLocked;
    public bool CanEditTask => !IsEditorReadOnly;
    
    public string Title
    {
        get => _title;
        set
        {
            if (!CanEditTask)
            {
                OnPropertyChanged();
                return;
            }
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
            if (IsTaskLocked)
            {
                OnPropertyChanged();
                return;
            }
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
                OnPropertyChanged(nameof(CanCompleteTask));
                OnPropertyChanged(nameof(TaskCompletionInfo));
                //RefreshEditingState();
            }
        }
    }

    public int Priority
    {
        get => _priority;
        set
        {
            if (!CanEditTask)
            {
                OnPropertyChanged();
                return;
            }
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
            if (!CanEditTask)
            {
                OnPropertyChanged();
                return;
            }
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

    public DateTimeOffset? Deadline
    {
        get => _deadline;
        set
        {
            if (!CanEditTask)
            {
                OnPropertyChanged();
                return;
            }
            if (SetProperty(ref _deadline, value))
            {
                if (_node.TaskNode != null)
                {
                    _node.TaskNode.Deadline = value ?? DateTimeOffset.MaxValue;
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
            if (!CanEditTask)
            {
                OnPropertyChanged();
                return;
            }
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
                OnPropertyChanged(nameof(CanCompleteTask));
                OnPropertyChanged(nameof(TaskCompletionInfo));
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
    public ObservableCollection<Resource> AvailableResources { get; }
    
    public ObservableCollection<TaskResourceUsageViewModel> ResourceUsages
    {
        get => _resourceUsages;
        set => SetProperty(ref _resourceUsages, value);
    }
    
    public Resource? SelectedResource
    {
        get => _selectedResource;
        set => SetProperty(ref _selectedResource, value);
    }
    
    public double ResourceAmount
    {
        get => _resourceAmount;
        set => SetProperty(ref _resourceAmount, value);
    }

    public string CompleteButtonText => _node.TaskNode?.IsCompleted == true 
        ? "Отменить выполнение" 
        : "Завершить задачу";

    public bool CanCompleteTask
    {
        get
        {
            if (_node.TaskNode == null || _mainViewModel == null)
                return false;

            if (!_mainViewModel.CanCompleteTasks)
                return false;

            // Если задача уже завершена, можем её отменить
            if (_node.TaskNode.IsCompleted)
                return true;

            // Проверяем, что задача назначена текущему пользователю (если не назначена — позволяем автоназначение)
            var currentActor = _mainViewModel.CurrentActor;
            if (currentActor == null)
                return false;

            var taskActor = _node.TaskNode.Meta?.PerfomedBy;
            if (taskActor == null)
                return true; // разрешаем, автоназначим в ToggleComplete

            return taskActor.Id == currentActor.Id;
        }
    }

    public string TaskCompletionInfo
    {
        get
        {
            if (_node.TaskNode == null || _mainViewModel == null)
                return string.Empty;

            if (_node.TaskNode.IsCompleted)
                return "✓ Задача завершена. Вы можете вернуть её в работу.";

            var currentActor = _mainViewModel.CurrentActor;
            var taskActor = _node.TaskNode.Meta?.PerfomedBy;

            if (!_mainViewModel.CanCompleteTasks)
                return "⚠ У вас нет прав на завершение задач";

            if (currentActor == null)
                return "⚠ Войдите в систему для завершения задачи";

            if (taskActor == null)
                return "⚠ Задача не назначена. Назначьте исполнителя для завершения.";

            if (taskActor.Id != currentActor.Id)
                return $"⚠ Задача назначена пользователю {taskActor.Name}. Только он может её завершить.";

            return $"✓ Вы можете завершить эту задачу";
        }
    }

    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddRelationCommand { get; }
    public ICommand CompleteTaskCommand { get; }
    public ICommand AddResourceCommand { get; }
    public ICommand ConfigureRecurringCommand { get; }

    public TaskEditorViewModel(GraphNode node, MainViewModel? mainViewModel, Graph? currentGraph)
    {
        _node = node;
        _mainViewModel = mainViewModel;
        _currentGraph = currentGraph;

        AvailableActors = new ObservableCollection<Actor>();
        AvailableNodes = new ObservableCollection<GraphNode>();
        AvailableResources = new ObservableCollection<Resource>();

        
        if (node.TaskNode != null)
        {
            _title = node.TaskNode.Title;
            _selectedStatus = node.TaskNode.Status;
            _priority = node.TaskNode.Priority;
            _payload = node.TaskNode.Payload ?? string.Empty;
            _deadline = node.TaskNode.Deadline != DateTimeOffset.MaxValue
                ? node.TaskNode.Deadline
                : null;
            _selectedActor = node.TaskNode.Meta?.PerfomedBy;
        }
        else
        {
            _title = node.Label;
        }

        // Автоматически назначаем задачу текущему пользователю, если она не назначена
        if (node.TaskNode != null && node.TaskNode.Meta?.PerfomedBy == null && mainViewModel?.CurrentActor != null)
        {
            mainViewModel.Yotei.Tasks.BindActor(node.TaskNode, mainViewModel.CurrentActor);
            _selectedActor = mainViewModel.CurrentActor;
        }

        
        if (mainViewModel != null)
        {
            foreach (var actor in mainViewModel.Yotei.Actors)
            {
                AvailableActors.Add(actor);
            }
            
            // Load available resources
            foreach (var resource in mainViewModel.Yotei.Resources.GetAll())
            {
                AvailableResources.Add(resource);
            }
        }

        
        if (currentGraph != null)
        {
            foreach (var availableNode in currentGraph.AvaibleNodes(node))
            {
                AvailableNodes.Add(availableNode);
            }
        }
        
        // Load existing resource usages
        foreach (var resourceUsage in node.ResourceUsages)
        {
            _resourceUsages.Add(new TaskResourceUsageViewModel(resourceUsage, DeleteResourceUsage, OnResourceAmountChanged));
        }

        AddRelationCommand = new RelayCommand(_ => AddRelation());
        CompleteTaskCommand = new RelayCommand(_ => ToggleComplete());
        AddResourceCommand = new RelayCommand(_ => AddResource());
        ConfigureRecurringCommand = new RelayCommand(_ => ConfigureRecurring());
        RefreshEditingState();
    }

    private void AddRelation()
    {
        if (!CanEditTask)
            return;
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
    
    private void AddResource()
    {
        if (!CanEditTask)
            return;
        if (_selectedResource == null || _resourceAmount <= 0)
            return;
        
        Console.WriteLine($"[TaskEditor] Добавление ресурса '{_selectedResource.Name}' (количество: {_resourceAmount}) к задаче '{_node.Label}'");
        
        // Check if resource already exists in the list
        var existingUsage = _node.ResourceUsages.FirstOrDefault(ru => ru.Resource.Id == _selectedResource.Id);
        if (existingUsage != null)
        {
            // Update existing amount
            existingUsage.Amount += _resourceAmount;
            var existingViewModel = _resourceUsages.FirstOrDefault(vm => vm.Model == existingUsage);
            if (existingViewModel != null)
            {
                existingViewModel.Amount = existingUsage.Amount;
            }
            Console.WriteLine($"[TaskEditor] Обновлено количество существующего ресурса: {existingUsage.Amount}");
        }
        else
        {
            // Add new resource usage
            var resourceUsage = new TaskResourceUsage(_selectedResource, _resourceAmount);
            _node.ResourceUsages.Add(resourceUsage);
            _resourceUsages.Add(new TaskResourceUsageViewModel(resourceUsage, DeleteResourceUsage, OnResourceAmountChanged));
            Console.WriteLine($"[TaskEditor] Добавлен новый ресурс. Всего ресурсов в задаче: {_node.ResourceUsages.Count}");
        }
        
        // Reset selection
        _selectedResource = null;
        _resourceAmount = 0;
        OnPropertyChanged(nameof(SelectedResource));
        OnPropertyChanged(nameof(ResourceAmount));
        
        _mainViewModel?.SyncGraphToRepository(_currentGraph);
        Console.WriteLine($"[TaskEditor] Вызов Save() для сохранения изменений...");
        _mainViewModel?.Save();
    }
    
    private void DeleteResourceUsage(TaskResourceUsageViewModel viewModel)
    {
        if (!CanEditTask)
            return;
        _node.ResourceUsages.Remove(viewModel.Model);
        _resourceUsages.Remove(viewModel);
        _mainViewModel?.SyncGraphToRepository(_currentGraph);
        _mainViewModel?.Save();
    }
    
    private void OnResourceAmountChanged()
    {
        if (!CanEditTask)
            return;
        _mainViewModel?.SyncGraphToRepository(_currentGraph);
        _mainViewModel?.Save();
    }

    private void ToggleComplete()
    {
        if (_node.TaskNode == null || _mainViewModel == null)
            return;

        if (!CanCompleteTask)
        {
            var currentActor = _mainViewModel.CurrentActor;
            var taskActor = _node.TaskNode.Meta?.PerfomedBy;
            
            if (!_mainViewModel.CanCompleteTasks)
            {
                Console.WriteLine("У вас нет прав на завершение задач");
            }
            else if (currentActor == null)
            {
                Console.WriteLine("Необходимо войти в систему для завершения задачи");
            }
            else if (taskActor == null)
            {
                Console.WriteLine("Задача не назначена ни одному пользователю");
            }
            else if (taskActor.Id != currentActor.Id)
            {
                Console.WriteLine($"Задача назначена пользователю {taskActor.Name}. Вы не можете завершить чужую задачу.");
            }
            return;
        }

        if (_node.TaskNode.IsCompleted)
        {
            try
            {
                var result = _mainViewModel.Yotei.Tasks.Uncomplete(_node.TaskNode.Id);
                if (result != null)
                {
                    Console.WriteLine($"Задача '{_node.TaskNode.Title}' возвращена в работу");
                }
                else
                {
                    Console.WriteLine($"Ошибка при отмене завершения задачи: {result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение при отмене завершения задачи: {ex.Message}");
                // Пытаемся установить статус напрямую
                _node.TaskNode.SetStatusSecure(TaskStatus.InProgress);
                Console.WriteLine($"Задача '{_node.TaskNode.Title}' возвращена в работу (через SetStatusSecure)");
            }
        }
        else
        {
            var result = _mainViewModel.Yotei.Tasks.TryComplete(_node.TaskNode, out var uncompleted);
            
            if (result == Result.OK)
            {
                Console.WriteLine($"Задача '{_node.TaskNode.Title}' успешно завершена пользователем {_mainViewModel.CurrentActor?.Name}");
                
                // Уведомляем RecurringTaskService о выполнении задачи
                _mainViewModel.OnTaskCompleted(_node);
            }
            else if (result == Result.ThereAreUncompletedTasks && uncompleted != null && uncompleted.Count > 0)
            {
                Console.WriteLine($"❌ Нельзя завершить задачу '{_node.TaskNode.Title}'");
                Console.WriteLine($"Сначала необходимо выполнить следующие задачи ({uncompleted.Count}):");
                
                // Получаем названия незавершенных задач
                var uncompletedTasks = new List<string>();
                foreach (var taskId in uncompleted)
                {
                    var task = _mainViewModel.Yotei.Tasks.GetAll().FirstOrDefault(t => t.Id == taskId);
                    if (task != null)
                    {
                        uncompletedTasks.Add($"  • '{task.Title}' (Статус: {task.Status})");
                    }
                    else
                    {
                        uncompletedTasks.Add($"  • Задача ID: {taskId}");
                    }
                }
                
                foreach (var taskInfo in uncompletedTasks)
                {
                    Console.WriteLine(taskInfo);
                }
            }
            else if (result == Result.WrongActor)
            {
                Console.WriteLine($"Ошибка: задача назначена другому пользователю");
            }
            else
            {
                Console.WriteLine($"Ошибка при завершении задачи: {result}");
            }
        }

        // Обновляем визуализацию и синхронизируем с репозиторием
        _node.SyncFromTaskNode();
        _node.RaiseVisualChanged();
        _mainViewModel?.SyncGraphToRepository(_currentGraph);

        // Обновляем статус в UI
        _selectedStatus = _node.TaskNode.Status;
        OnPropertyChanged(nameof(SelectedStatus));
        OnPropertyChanged(nameof(CompleteButtonText));
        OnPropertyChanged(nameof(CanCompleteTask));
        OnPropertyChanged(nameof(TaskCompletionInfo));
        RefreshEditingState();
    }

    private void ConfigureRecurring()
    {
        _mainViewModel?.ShowRecurringTaskConfig(_node);
    }

    private void RefreshEditingState()
    {
        OnPropertyChanged(nameof(IsTaskLocked));
        OnPropertyChanged(nameof(IsEditorReadOnly));
        OnPropertyChanged(nameof(CanEditTask));
        OnPropertyChanged(nameof(CanCompleteTask));
        OnPropertyChanged(nameof(TaskCompletionInfo));
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


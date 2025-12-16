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




public class ProjectCreationPopupViewModel : INotifyPropertyChanged
{
    private string _projectName = string.Empty;
    private ObservableCollection<ProjectActorRoleViewModel> _actorRoles = new();
    private readonly ObservableCollection<Actor> _availableActors;
    private readonly ObservableCollection<YoteiLib.Core.Role> _availableRoles;

    public string ProjectName
    {
        get => _projectName;
        set
        {
            if (_projectName != value)
            {
                _projectName = value;
                OnPropertyChanged();
                (CreateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<ProjectActorRoleViewModel> ActorRoles
    {
        get => _actorRoles;
        set => SetProperty(ref _actorRoles, value);
    }

    public ObservableCollection<Actor> AvailableActors => _availableActors;
    public ObservableCollection<YoteiLib.Core.Role> AvailableRoles => _availableRoles;

    public ICommand AddActorRoleCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler<Project?>? ProjectCreated;

    public ProjectCreationPopupViewModel(
        IEnumerable<Actor> actors,
        IEnumerable<YoteiLib.Core.Role> roles)
    {
        _availableActors = new ObservableCollection<Actor>(actors);
        _availableRoles = new ObservableCollection<YoteiLib.Core.Role>(roles);

        AddActorRoleCommand = new RelayCommand(_ => AddActorRole());
        CreateCommand = new RelayCommand(_ => CreateProject(), _ => CanCreateProject());
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    private void AddActorRole()
    {
        var newActorRole = new ProjectActorRoleViewModel(
            _availableActors.ToList(),
            _availableRoles.ToList(),
            RemoveActorRole);
        
        _actorRoles.Add(newActorRole);
    }

    private void RemoveActorRole(ProjectActorRoleViewModel actorRole)
    {
        _actorRoles.Remove(actorRole);
    }

    private bool CanCreateProject()
    {
        return !string.IsNullOrWhiteSpace(_projectName);
    }

    private void CreateProject()
    {
        if (!CanCreateProject())
            return;

        
        var graph = new Graph(_projectName);

        
        var project = new Project(_projectName, graph);

        
        foreach (var actorRole in _actorRoles)
        {
            if (actorRole.SelectedActor != null && actorRole.SelectedRole != null)
            {
                project.AddActorRole(
                    actorRole.SelectedActor.Id.ToString(),
                    actorRole.SelectedRole.Id.ToString());
            }
        }

        ProjectCreated?.Invoke(this, project);
    }

    private void Cancel()
    {
        ProjectCreated?.Invoke(this, null);
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




public class ProjectActorRoleViewModel : INotifyPropertyChanged
{
    private Actor? _selectedActor;
    private YoteiLib.Core.Role? _selectedRole;
    private readonly List<Actor> _availableActors;
    private readonly List<YoteiLib.Core.Role> _availableRoles;
    private readonly Action<ProjectActorRoleViewModel> _removeCallback;

    public Actor? SelectedActor
    {
        get => _selectedActor;
        set => SetProperty(ref _selectedActor, value);
    }

    public YoteiLib.Core.Role? SelectedRole
    {
        get => _selectedRole;
        set => SetProperty(ref _selectedRole, value);
    }

    public List<Actor> AvailableActors => _availableActors;
    public List<YoteiLib.Core.Role> AvailableRoles => _availableRoles;

    public ICommand RemoveCommand { get; }

    public ProjectActorRoleViewModel(
        List<Actor> availableActors,
        List<YoteiLib.Core.Role> availableRoles,
        Action<ProjectActorRoleViewModel> removeCallback)
    {
        _availableActors = availableActors;
        _availableRoles = availableRoles;
        _removeCallback = removeCallback;

        RemoveCommand = new RelayCommand(_ => _removeCallback(this));
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

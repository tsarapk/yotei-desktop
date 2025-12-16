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




public class ProjectEditorViewModel : INotifyPropertyChanged
{
    private readonly Project _project;
    private string _projectName;
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
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler<bool>? EditCompleted;

    public ProjectEditorViewModel(
        Project project,
        IEnumerable<Actor> actors,
        IEnumerable<YoteiLib.Core.Role> roles)
    {
        _project = project;
        _projectName = project.Name;
        _availableActors = new ObservableCollection<Actor>(actors);
        _availableRoles = new ObservableCollection<YoteiLib.Core.Role>(roles);

        
        Console.WriteLine($"ProjectEditorViewModel создан для проекта: {project.Name} (ID: {project.Id})");

        
        LoadExistingActorRoles();

        AddActorRoleCommand = new RelayCommand(_ => AddActorRole());
        SaveCommand = new RelayCommand(_ => SaveChanges(), _ => CanSave());
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    private void LoadExistingActorRoles()
    {
        foreach (var actorRole in _project.ActorRoles)
        {
            var actor = _availableActors.FirstOrDefault(a => a.Id.ToString() == actorRole.ActorId);
            var role = _availableRoles.FirstOrDefault(r => r.Id.ToString() == actorRole.RoleId);

            if (actor != null && role != null)
            {
                var viewModel = new ProjectActorRoleViewModel(
                    _availableActors.ToList(),
                    _availableRoles.ToList(),
                    RemoveActorRole)
                {
                    SelectedActor = actor,
                    SelectedRole = role
                };

                _actorRoles.Add(viewModel);
            }
        }
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

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(_projectName);
    }

    private void SaveChanges()
    {
        if (!CanSave())
            return;

        
        _project.Name = _projectName;

        
        _project.ActorRoles.Clear();

        
        foreach (var actorRole in _actorRoles)
        {
            if (actorRole.SelectedActor != null && actorRole.SelectedRole != null)
            {
                _project.AddActorRole(
                    actorRole.SelectedActor.Id.ToString(),
                    actorRole.SelectedRole.Id.ToString());
            }
        }

        EditCompleted?.Invoke(this, true);
    }

    private void Cancel()
    {
        EditCompleted?.Invoke(this, false);
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

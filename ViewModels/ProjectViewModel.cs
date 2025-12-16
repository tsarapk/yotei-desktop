using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YoteiTasks.Models;

namespace YoteiTasks.ViewModels;




public class ProjectViewModel : INotifyPropertyChanged
{
    private readonly Project _project;
    private readonly Action<ProjectViewModel>? _deleteCallback;

    public ProjectViewModel(Project project, Action<ProjectViewModel>? deleteCallback = null)
    {
        _project = project;
        _deleteCallback = deleteCallback;
        DeleteCommand = new RelayCommand(_ => _deleteCallback?.Invoke(this));
    }

    public Project Model => _project;

    public string Name
    {
        get => _project.Name;
        set
        {
            if (_project.Name != value)
            {
                _project.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand DeleteCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

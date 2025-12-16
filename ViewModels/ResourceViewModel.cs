using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YoteiLib.Core;

namespace YoteiTasks.ViewModels;

public class ResourceViewModel : INotifyPropertyChanged
{
    private readonly Resource _resource;
    private readonly Action<ResourceViewModel>? _deleteCallback;

    public ICommand DeleteCommand { get; }

    public ResourceViewModel(Resource resource, Action<ResourceViewModel>? deleteCallback = null)
    {
        _resource = resource;
        _deleteCallback = deleteCallback;
        DeleteCommand = new RelayCommand(_ => _deleteCallback?.Invoke(this));
    }

    public Resource Model => _resource;

    public string Id => _resource.Id.ToString();

    public string Name
    {
        get => _resource.Name;
        set
        {
            if (_resource.Name != value)
            {
                _resource.SetName(value ?? string.Empty);
                OnPropertyChanged();
            }
        }
    }

    public double Value
    {
        get => _resource.Value;
        set
        {
            if (_resource.Value != value)
            {
                _resource.SetValue(value);
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}



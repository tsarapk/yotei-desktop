using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YoteiLib.Core;

namespace YoteiTasks.Models;

/// <summary>
/// Represents resource usage in a task
/// </summary>
public class TaskResourceUsage : INotifyPropertyChanged
{
    private Resource _resource;
    private double _amount;

    public Resource Resource
    {
        get => _resource;
        set
        {
            if (_resource != value)
            {
                _resource = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResourceName));
            }
        }
    }

    public double Amount
    {
        get => _amount;
        set
        {
            if (_amount != value)
            {
                _amount = value;
                OnPropertyChanged();
            }
        }
    }

    public string ResourceName => _resource?.Name ?? string.Empty;

    public TaskResourceUsage(Resource resource, double amount)
    {
        _resource = resource;
        _amount = amount;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

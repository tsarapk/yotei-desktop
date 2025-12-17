using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YoteiLib.Core;
using YoteiTasks.Models;

namespace YoteiTasks.ViewModels;

/// <summary>
/// ViewModel for managing resource usage in task editor
/// </summary>
public class TaskResourceUsageViewModel : INotifyPropertyChanged
{
    private readonly TaskResourceUsage _model;
    private readonly Action<TaskResourceUsageViewModel>? _deleteCallback;
    private readonly Action? _onAmountChanged;

    public TaskResourceUsageViewModel(TaskResourceUsage model, Action<TaskResourceUsageViewModel>? deleteCallback = null, Action? onAmountChanged = null)
    {
        _model = model;
        _deleteCallback = deleteCallback;
        _onAmountChanged = onAmountChanged;
        DeleteCommand = new RelayCommand(_ => _deleteCallback?.Invoke(this));
    }

    public TaskResourceUsage Model => _model;

    public string ResourceName => _model.ResourceName;

    public double Amount
    {
        get => _model.Amount;
        set
        {
            if (_model.Amount != value)
            {
                _model.Amount = value;
                OnPropertyChanged();
                _onAmountChanged?.Invoke();
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

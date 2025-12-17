using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YoteiTasks.Models;

namespace YoteiTasks.ViewModels;

/// <summary>
/// ViewModel for configuring recurring tasks
/// </summary>
public class RecurringTaskConfigViewModel : INotifyPropertyChanged
{
    private readonly RecurringTaskConfig _config;
    private readonly Action? _closeCallback;
    private readonly Action<RecurringTaskConfig>? _saveCallback;
    private string _taskName;

    public RecurringTaskConfigViewModel(
        string taskName,
        RecurringTaskConfig config,
        Action? closeCallback = null,
        Action<RecurringTaskConfig>? saveCallback = null)
    {
        _taskName = taskName;
        _config = config;
        _closeCallback = closeCallback;
        _saveCallback = saveCallback;

        SaveCommand = new RelayCommand(_ => Save());
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    public string TaskName
    {
        get => _taskName;
        set => SetProperty(ref _taskName, value);
    }

    public RecurrenceType RecurrenceType
    {
        get => _config.RecurrenceType;
        set
        {
            if (_config.RecurrenceType != value)
            {
                _config.RecurrenceType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRecurring));
                OnPropertyChanged(nameof(ShowInterval));
                OnPropertyChanged(nameof(IntervalLabel));
            }
        }
    }

    public int Interval
    {
        get => _config.Interval;
        set
        {
            if (_config.Interval != value && value > 0)
            {
                _config.Interval = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AutoReset
    {
        get => _config.AutoReset;
        set
        {
            if (_config.AutoReset != value)
            {
                _config.AutoReset = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowAutoResetDelay));
            }
        }
    }

    public int AutoResetDelayMinutes
    {
        get => (int)(_config.AutoResetDelay?.TotalMinutes ?? 5);
        set
        {
            if (value > 0)
            {
                _config.AutoResetDelay = TimeSpan.FromMinutes(value);
                OnPropertyChanged();
            }
        }
    }

    public bool NotificationsEnabled
    {
        get => _config.NotificationsEnabled;
        set
        {
            if (_config.NotificationsEnabled != value)
            {
                _config.NotificationsEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowNotificationAdvance));
            }
        }
    }

    public int NotificationAdvanceMinutes
    {
        get => (int?)_config.NotificationAdvance?.TotalMinutes ?? 0;
        set
        {
            if (value >= 0)
            {
                _config.NotificationAdvance = value > 0 ? TimeSpan.FromMinutes(value) : null;
                OnPropertyChanged();
            }
        }
    }

    public bool IsRecurring => RecurrenceType != RecurrenceType.None;
    public bool ShowInterval => RecurrenceType == RecurrenceType.Minutes || 
                                RecurrenceType == RecurrenceType.Hours ||
                                RecurrenceType == RecurrenceType.Daily ||
                                RecurrenceType == RecurrenceType.Weekly ||
                                RecurrenceType == RecurrenceType.Monthly;
    
    public bool ShowAutoResetDelay => AutoReset;
    public bool ShowNotificationAdvance => NotificationsEnabled;

    public string IntervalLabel => RecurrenceType switch
    {
        RecurrenceType.Minutes => "Интервал (минуты):",
        RecurrenceType.Hours => "Интервал (часы):",
        RecurrenceType.Daily => "Каждые N дней:",
        RecurrenceType.Weekly => "Каждые N недель:",
        RecurrenceType.Monthly => "Каждые N месяцев:",
        _ => "Интервал:"
    };

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private void Save()
    {
        // Вычисляем первую дату выполнения
        if (_config.RecurrenceType != RecurrenceType.None && _config.NextDueDate == null)
        {
            _config.NextDueDate = _config.CalculateNextDueDate(DateTimeOffset.Now);
        }

        _saveCallback?.Invoke(_config);
        _closeCallback?.Invoke();
    }

    private void Cancel()
    {
        _closeCallback?.Invoke();
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

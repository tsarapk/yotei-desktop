using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace YoteiTasks.Services;




public class NotificationService : INotifyPropertyChanged
{
    private static NotificationService? _instance;
    private ObservableCollection<Notification> _notifications = new();

    public static NotificationService Instance => _instance ??= new NotificationService();

    public ObservableCollection<Notification> Notifications
    {
        get => _notifications;
        private set
        {
            _notifications = value;
            OnPropertyChanged();
        }
    }

    private NotificationService()
    {
    }

    
    
    
    public void ShowSuccess(string message, int durationMs = 3000)
    {
        Show(message, NotificationType.Success, durationMs);
    }

    
    
    
    public void ShowInfo(string message, int durationMs = 3000)
    {
        Show(message, NotificationType.Info, durationMs);
    }

    
    
    
    public void ShowWarning(string message, int durationMs = 4000)
    {
        Show(message, NotificationType.Warning, durationMs);
    }

    
    
    
    public void ShowError(string message, int durationMs = 5000)
    {
        Show(message, NotificationType.Error, durationMs);
    }

    private void Show(string message, NotificationType type, int durationMs)
    {
        var notification = new Notification
        {
            Message = message,
            Type = type,
            Timestamp = DateTime.Now
        };

        Notifications.Add(notification);

        
        Task.Delay(durationMs).ContinueWith(_ =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Notifications.Remove(notification);
            });
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}




public class Notification
{
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime Timestamp { get; set; }
}




public enum NotificationType
{
    Success,
    Info,
    Warning,
    Error
}

using System;

namespace YoteiTasks.Models;

/// <summary>
/// Recurrence pattern for tasks
/// </summary>
public enum RecurrenceType
{
    None,           // Не повторяется
    Minutes,        // Каждые N минут
    Hours,          // Каждые N часов
    Daily,          // Каждый день
    Weekly,         // Каждую неделю
    Monthly,        // Каждый месяц
    Custom          // Пользовательский интервал
}

/// <summary>
/// Recurring task configuration
/// </summary>
public class RecurringTaskConfig
{
    public string NodeId { get; set; } = string.Empty;
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public int Interval { get; set; } = 1; // Интервал повторения (например, каждые 5 минут)
    public bool AutoReset { get; set; } = false; // Автоматически сбрасывать выполнение
    public TimeSpan? AutoResetDelay { get; set; } // Задержка перед сбросом (например, 5 минут)
    public bool NotificationsEnabled { get; set; } = true;
    public TimeSpan? NotificationAdvance { get; set; } // За сколько до дедлайна уведомлять
    public DateTimeOffset? LastNotification { get; set; }
    public DateTimeOffset? LastReset { get; set; }
    public DateTimeOffset? NextDueDate { get; set; }
    
    public RecurringTaskConfig() { }
    
    public RecurringTaskConfig(string nodeId)
    {
        NodeId = nodeId;
    }

    /// <summary>
    /// Calculate next due date based on recurrence pattern
    /// </summary>
    public DateTimeOffset CalculateNextDueDate(DateTimeOffset from)
    {
        return RecurrenceType switch
        {
            RecurrenceType.Minutes => from.AddMinutes(Interval),
            RecurrenceType.Hours => from.AddHours(Interval),
            RecurrenceType.Daily => from.AddDays(Interval),
            RecurrenceType.Weekly => from.AddDays(Interval * 7),
            RecurrenceType.Monthly => from.AddMonths(Interval),
            _ => from
        };
    }

    /// <summary>
    /// Check if notification should be sent
    /// </summary>
    public bool ShouldNotify(DateTimeOffset now)
    {
        if (!NotificationsEnabled || NextDueDate == null)
            return false;

        // Если уже отправляли уведомление недавно, не отправлять снова
        if (LastNotification.HasValue && (now - LastNotification.Value).TotalMinutes < 1)
            return false;

        // Если есть advance notice, проверяем заранее
        if (NotificationAdvance.HasValue)
        {
            var notifyTime = NextDueDate.Value - NotificationAdvance.Value;
            return now >= notifyTime && now < NextDueDate.Value;
        }

        // Иначе уведомляем когда наступил срок
        return now >= NextDueDate.Value;
    }

    /// <summary>
    /// Check if task should be reset
    /// </summary>
    public bool ShouldReset(DateTimeOffset now, bool isCompleted)
    {
        if (!AutoReset || !isCompleted || !AutoResetDelay.HasValue)
            return false;

        if (!LastReset.HasValue)
            return false;

        var resetTime = LastReset.Value + AutoResetDelay.Value;
        return now >= resetTime;
    }
}
